using CommissionSettlement.Common;
using CommissionSettlement.Data;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CommissionSettlement.Services;

public interface ISettlementService
{
    Task<List<MonthlySettlementDto>> GenerateMonthlySettlementsAsync(string settlementMonth, Guid generatedByUserId, CancellationToken cancellationToken = default);
    Task<MonthlySettlementDto?> GetSettlementByIdAsync(Guid settlementId, bool includeSnapshots = false, CancellationToken cancellationToken = default);
    Task<PagedResult<MonthlySettlementDto>> QuerySettlementsAsync(SettlementQueryDto query, CancellationToken cancellationToken = default);
    Task<MonthlySettlementDto> ApproveSettlementAsync(Guid settlementId, Guid approvedByUserId, CancellationToken cancellationToken = default);
    Task<MonthlySettlementDto> RejectSettlementAsync(Guid settlementId, Guid rejectedByUserId, string reason, CancellationToken cancellationToken = default);
    Task<MonthlySettlementDto> MarkAsPaidAsync(Guid settlementId, Guid paidByUserId, CancellationToken cancellationToken = default);
    Task<List<SettlementSnapshotDto>> GetSettlementSnapshotsAsync(Guid settlementId, CancellationToken cancellationToken = default);
    Task<List<ClawbackRecordDto>> GetClawbackRecordsAsync(Guid? policyId = null, CancellationToken cancellationToken = default);
    Task<List<MonthlySettlementDto>> GetUserSettlementsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class SettlementService : ISettlementService
{
    private readonly AppDbContext _context;

    public SettlementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MonthlySettlementDto>> GenerateMonthlySettlementsAsync(string settlementMonth, Guid generatedByUserId, CancellationToken cancellationToken = default)
    {
        if (!TryParseMonth(settlementMonth, out var monthDate))
            throw new InvalidOperationException("结算月份格式无效，应为 YYYY-MM");

        var financeUser = await _context.Users.FindAsync(new object?[] { generatedByUserId }, cancellationToken);
        if (financeUser == null || (financeUser.Role != UserRole.Finance && financeUser.Role != UserRole.Admin))
            throw new InvalidOperationException("只有财务人员或管理员可以生成月度结算单");

        var existing = await _context.MonthlySettlements
            .Where(s => s.SettlementMonth == settlementMonth && s.Status != SettlementStatus.Draft)
            .ToListAsync(cancellationToken);
        if (existing.Any())
            throw new InvalidOperationException($"{settlementMonth} 的结算单已生成，如需重新生成请先删除现有草稿");

        var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var effectivePolicies = await _context.Policies
            .Include(p => p.AllocationRules)
                .ThenInclude(r => r.Details)
                .ThenInclude(d => d.User)
            .Include(p => p.AgentUser)
            .Where(p => p.Status == PolicyStatus.Effective
                && p.EffectiveAt.HasValue
                && p.EffectiveAt.Value <= monthEnd)
            .ToListAsync(cancellationToken);

        var clawbacks = await _context.ClawbackRecords
            .Where(c => c.CancelledAt >= monthStart && c.CancelledAt <= monthEnd)
            .ToListAsync(cancellationToken);

        var deductions = await _context.PreTaxDeductions
            .Where(d => d.DeductionMonth == settlementMonth)
            .Include(d => d.User)
            .ToListAsync(cancellationToken);

        var userCommissions = new Dictionary<Guid, UserCommissionAccumulator>();

        foreach (var policy in effectivePolicies)
        {
            var rule = policy.AllocationRules
                .FirstOrDefault(r => r.EffectiveStartDate <= monthEnd
                    && (!r.EffectiveEndDate.HasValue || r.EffectiveEndDate.Value >= monthStart));

            if (rule == null || !rule.Details.Any()) continue;

            var policyClawback = clawbacks.FirstOrDefault(c => c.PolicyId == policy.PolicyId);

            foreach (var detail in rule.Details)
            {
                if (!userCommissions.ContainsKey(detail.UserId))
                {
                    userCommissions[detail.UserId] = new UserCommissionAccumulator();
                }

                var accum = userCommissions[detail.UserId];
                var allocatedCommission = Math.Round(policy.CommissionAmount * detail.AllocationRatio, 2);

                var clawbackShare = policyClawback != null
                    ? Math.Round(policyClawback.ClawbackAmount * detail.AllocationRatio, 2)
                    : 0m;

                accum.TotalCommission += allocatedCommission;
                accum.TotalClawback += clawbackShare;

                var snapshot = new SnapshotBuildItem
                {
                    Policy = policy,
                    Rule = rule,
                    Detail = detail,
                    AllocatedCommission = allocatedCommission,
                    ClawbackShare = clawbackShare
                };
                accum.Snapshots.Add(snapshot);
            }
        }

        foreach (var deduction in deductions)
        {
            if (!userCommissions.ContainsKey(deduction.UserId))
            {
                userCommissions[deduction.UserId] = new UserCommissionAccumulator();
            }
            userCommissions[deduction.UserId].TotalPreTaxDeduction += deduction.DeductionAmount;
        }

        var resultSettlements = new List<MonthlySettlement>();

        foreach (var (userId, accum) in userCommissions)
        {
            var user = await _context.Users.FindAsync(new object?[] { userId }, cancellationToken);
            if (user == null || !user.IsActive) continue;

            var taxableIncome = Math.Max(0, accum.TotalCommission - accum.TotalClawback - accum.TotalPreTaxDeduction);
            var incomeTax = CalculateIncomeTax(taxableIncome);
            var netPayable = Math.Max(0, taxableIncome - incomeTax);

            var settlement = new MonthlySettlement
            {
                SettlementId = Guid.NewGuid(),
                SettlementMonth = settlementMonth,
                UserId = userId,
                TotalCommission = Math.Round(accum.TotalCommission, 2),
                TotalClawback = Math.Round(accum.TotalClawback, 2),
                TotalPreTaxDeduction = Math.Round(accum.TotalPreTaxDeduction, 2),
                TaxableIncome = Math.Round(taxableIncome, 2),
                IncomeTax = Math.Round(incomeTax, 2),
                NetPayable = Math.Round(netPayable, 2),
                Status = SettlementStatus.Generated,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.UtcNow
            };

            settlement.Snapshots = BuildSnapshots(settlement, accum);

            _context.MonthlySettlements.Add(settlement);
            resultSettlements.Add(settlement);
        }

        foreach (var clawback in clawbacks.Where(c => c.AffectedSettlementId == null))
        {
            var affected = resultSettlements.FirstOrDefault(s =>
                s.Snapshots.Any(snap => snap.PolicyId == clawback.PolicyId));
            if (affected != null)
                clawback.AffectedSettlementId = affected.SettlementId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return resultSettlements.Select(s => MapToDto(s, true)).ToList();
    }

    private static List<SettlementSnapshot> BuildSnapshots(MonthlySettlement settlement, UserCommissionAccumulator accum)
    {
        return accum.Snapshots.Select(item => new SettlementSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            SettlementId = settlement.SettlementId,
            PolicyId = item.Policy.PolicyId,
            PolicyNo = item.Policy.PolicyNo,
            PolicyStatus = item.Policy.Status.ToString(),
            Premium = item.Policy.Premium,
            CommissionRate = item.Policy.CommissionRate,
            OriginalCommission = item.Policy.CommissionAmount,
            AllocationRatio = item.Detail.AllocationRatio,
            AllocatedCommission = item.AllocatedCommission,
            ClawbackAmount = item.ClawbackShare,
            PreTaxDeduction = 0m,
            RuleSnapshot = JsonConvert.SerializeObject(new
            {
                RuleId = item.Rule.RuleId,
                EffectiveStartDate = item.Rule.EffectiveStartDate,
                item.Detail.UserId,
                item.Detail.RoleType,
                item.Detail.AllocationRatio
            }),
            CreatedAt = DateTime.UtcNow
        }).ToList();
    }

    private static decimal CalculateIncomeTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0;
        if (taxableIncome <= 3000) return taxableIncome * 0.03m;
        if (taxableIncome <= 12000) return taxableIncome * 0.10m - 210;
        if (taxableIncome <= 25000) return taxableIncome * 0.20m - 1410;
        if (taxableIncome <= 35000) return taxableIncome * 0.25m - 2660;
        if (taxableIncome <= 55000) return taxableIncome * 0.30m - 4410;
        if (taxableIncome <= 80000) return taxableIncome * 0.35m - 7160;
        return taxableIncome * 0.45m - 15160;
    }

    private static bool TryParseMonth(string month, out DateTime result)
    {
        return DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null,
            System.Globalization.DateTimeStyles.None, out result);
    }

    public async Task<MonthlySettlementDto?> GetSettlementByIdAsync(Guid settlementId, bool includeSnapshots = false, CancellationToken cancellationToken = default)
    {
        var query = _context.MonthlySettlements.Include(s => s.User).AsQueryable();
        if (includeSnapshots)
            query = query.Include(s => s.Snapshots);

        var settlement = await query.FirstOrDefaultAsync(s => s.SettlementId == settlementId, cancellationToken);
        return settlement == null ? null : MapToDto(settlement, includeSnapshots);
    }

    public async Task<PagedResult<MonthlySettlementDto>> QuerySettlementsAsync(SettlementQueryDto query, CancellationToken cancellationToken = default)
    {
        var q = _context.MonthlySettlements.Include(s => s.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SettlementMonth))
            q = q.Where(s => s.SettlementMonth == query.SettlementMonth);
        if (query.UserId.HasValue)
            q = q.Where(s => s.UserId == query.UserId.Value);
        if (query.Status.HasValue)
            q = q.Where(s => s.Status == query.Status.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(s => s.GeneratedAt)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => MapToDto(s, false))
            .ToListAsync(cancellationToken);

        return new PagedResult<MonthlySettlementDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    public async Task<MonthlySettlementDto> ApproveSettlementAsync(Guid settlementId, Guid approvedByUserId, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.MonthlySettlements.FindAsync(new object?[] { settlementId }, cancellationToken);
        if (settlement == null)
            throw new InvalidOperationException("结算单不存在");
        if (settlement.Status != SettlementStatus.Generated)
            throw new InvalidOperationException("只有已生成状态的结算单可以审批");

        settlement.Status = SettlementStatus.Approved;
        settlement.ApprovedAt = DateTime.UtcNow;
        settlement.ApprovedByUserId = approvedByUserId;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetSettlementByIdAsync(settlementId, true, cancellationToken)
            ?? throw new InvalidOperationException("审批后查询失败");
    }

    public async Task<MonthlySettlementDto> RejectSettlementAsync(Guid settlementId, Guid rejectedByUserId, string reason, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.MonthlySettlements.FindAsync(new object?[] { settlementId }, cancellationToken);
        if (settlement == null)
            throw new InvalidOperationException("结算单不存在");
        if (settlement.Status != SettlementStatus.Generated)
            throw new InvalidOperationException("只有已生成状态的结算单可以驳回");

        settlement.Status = SettlementStatus.Rejected;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetSettlementByIdAsync(settlementId, true, cancellationToken)
            ?? throw new InvalidOperationException("驳回后查询失败");
    }

    public async Task<MonthlySettlementDto> MarkAsPaidAsync(Guid settlementId, Guid paidByUserId, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.MonthlySettlements.FindAsync(new object?[] { settlementId }, cancellationToken);
        if (settlement == null)
            throw new InvalidOperationException("结算单不存在");
        if (settlement.Status != SettlementStatus.Approved)
            throw new InvalidOperationException("只有已审批状态的结算单可以标记已支付");

        settlement.Status = SettlementStatus.Paid;
        settlement.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await GetSettlementByIdAsync(settlementId, true, cancellationToken)
            ?? throw new InvalidOperationException("标记支付后查询失败");
    }

    public async Task<List<SettlementSnapshotDto>> GetSettlementSnapshotsAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var snapshots = await _context.SettlementSnapshots
            .Where(s => s.SettlementId == settlementId)
            .OrderBy(s => s.PolicyNo)
            .ToListAsync(cancellationToken);

        return snapshots.Select(MapSnapshotToDto).ToList();
    }

    public async Task<List<ClawbackRecordDto>> GetClawbackRecordsAsync(Guid? policyId = null, CancellationToken cancellationToken = default)
    {
        var q = _context.ClawbackRecords.Include(c => c.Policy).AsQueryable();
        if (policyId.HasValue)
            q = q.Where(c => c.PolicyId == policyId.Value);

        var records = await q.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
        return records.Select(MapClawbackToDto).ToList();
    }

    public async Task<List<MonthlySettlementDto>> GetUserSettlementsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settlements = await _context.MonthlySettlements
            .Include(s => s.User)
            .Include(s => s.Snapshots)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SettlementMonth)
            .ToListAsync(cancellationToken);

        return settlements.Select(s => MapToDto(s, true)).ToList();
    }

    private static MonthlySettlementDto MapToDto(MonthlySettlement s, bool includeSnapshots) => new(
        s.SettlementId,
        s.SettlementMonth,
        s.UserId,
        s.User?.UserName ?? string.Empty,
        s.User?.UserCode ?? string.Empty,
        s.TotalCommission,
        s.TotalClawback,
        s.TotalPreTaxDeduction,
        s.TaxableIncome,
        s.IncomeTax,
        s.NetPayable,
        s.Status,
        s.GeneratedAt,
        s.ApprovedAt,
        s.PaidAt,
        includeSnapshots ? s.Snapshots.Select(MapSnapshotToDto).ToList() : null
    );

    private static SettlementSnapshotDto MapSnapshotToDto(SettlementSnapshot s) => new(
        s.SnapshotId,
        s.SettlementId,
        s.PolicyId,
        s.PolicyNo,
        s.PolicyStatus,
        s.Premium,
        s.CommissionRate,
        s.OriginalCommission,
        s.AllocationRatio,
        s.AllocatedCommission,
        s.ClawbackAmount,
        s.PreTaxDeduction,
        s.RuleSnapshot,
        s.CreatedAt
    );

    private static ClawbackRecordDto MapClawbackToDto(ClawbackRecord c) => new(
        c.ClawbackId,
        c.PolicyId,
        c.Policy?.PolicyNo ?? string.Empty,
        c.ClawbackType,
        c.ClawbackAmount,
        c.CancelledAt,
        c.Reason,
        c.IsCoolingPeriod,
        c.CreatedAt
    );

    private class UserCommissionAccumulator
    {
        public decimal TotalCommission { get; set; }
        public decimal TotalClawback { get; set; }
        public decimal TotalPreTaxDeduction { get; set; }
        public List<SnapshotBuildItem> Snapshots { get; } = new();
    }

    private class SnapshotBuildItem
    {
        public Policy Policy { get; set; } = null!;
        public AllocationRule Rule { get; set; } = null!;
        public AllocationRuleDetail Detail { get; set; } = null!;
        public decimal AllocatedCommission { get; set; }
        public decimal ClawbackShare { get; set; }
    }
}
