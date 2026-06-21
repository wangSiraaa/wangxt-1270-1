using CommissionSettlement.Common;
using CommissionSettlement.Data;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CommissionSettlement.Services;

public interface IAllocationService
{
    Task<AllocationRuleDto?> GetActiveRuleByPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<List<AllocationRuleDto>> GetPolicyRulesAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<List<AllocationAdjustmentDto>> GetPolicyAdjustmentHistoryAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<AllocationAdjustmentDto> AdjustAllocationAsync(AdjustAllocationDto dto, Guid adjustedByUserId, CancellationToken cancellationToken = default);
    Task<AllocationRuleDto?> GetRuleForMonthAsync(Guid policyId, DateTime settlementMonth, CancellationToken cancellationToken = default);
}

public class AllocationService : IAllocationService
{
    private readonly AppDbContext _context;

    public AllocationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AllocationRuleDto?> GetActiveRuleByPolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.AllocationRules
            .Include(r => r.Details)
            .ThenInclude(d => d.User)
            .Include(r => r.Policy)
            .FirstOrDefaultAsync(r => r.PolicyId == policyId && r.IsActive && r.EffectiveEndDate == null, cancellationToken);

        return rule == null ? null : MapToDto(rule);
    }

    public async Task<List<AllocationRuleDto>> GetPolicyRulesAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var rules = await _context.AllocationRules
            .Include(r => r.Details)
            .ThenInclude(d => d.User)
            .Include(r => r.Policy)
            .Where(r => r.PolicyId == policyId)
            .OrderBy(r => r.EffectiveStartDate)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<List<AllocationAdjustmentDto>> GetPolicyAdjustmentHistoryAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var adjustments = await _context.AllocationAdjustments
            .Include(a => a.Policy)
            .Include(a => a.OldRule)
                .ThenInclude(r => r!.Details)
                .ThenInclude(d => d.User)
            .Include(a => a.NewRule)
                .ThenInclude(r => r.Details)
                .ThenInclude(d => d.User)
            .Where(a => a.PolicyId == policyId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return adjustments.Select(MapAdjustmentToDto).ToList();
    }

    public async Task<AllocationAdjustmentDto> AdjustAllocationAsync(AdjustAllocationDto dto, Guid adjustedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.AdjustmentReason))
            throw new InvalidOperationException("分摊比例调整必须填写原因");

        if (dto.NewDetails.Sum(d => d.AllocationRatio) != 1.0m)
            throw new InvalidOperationException("所有人员分摊比例之和必须等于100%");

        var policy = await _context.Policies
            .Include(p => p.AllocationRules)
            .ThenInclude(r => r.Details)
            .FirstOrDefaultAsync(p => p.PolicyId == dto.PolicyId, cancellationToken);

        if (policy == null)
            throw new InvalidOperationException("保单不存在");

        if (dto.EffectiveFromDate < DateTime.UtcNow.Date)
            throw new InvalidOperationException("生效日期不能早于今天，禁止改写历史数据");

        var supervisor = await _context.Users.FindAsync(new object?[] { adjustedByUserId }, cancellationToken);
        if (supervisor == null || (supervisor.Role != UserRole.Supervisor && supervisor.Role != UserRole.Admin))
            throw new InvalidOperationException("只有团队主管或管理员可以调整分摊比例");

        var currentActiveRule = policy.AllocationRules
            .FirstOrDefault(r => r.IsActive && r.EffectiveEndDate == null);

        var newRule = new AllocationRule
        {
            RuleId = Guid.NewGuid(),
            PolicyId = dto.PolicyId,
            EffectiveStartDate = dto.EffectiveFromDate,
            IsActive = true,
            CreatedByUserId = adjustedByUserId,
            CreatedAt = DateTime.UtcNow,
            Details = dto.NewDetails.Select(d => new AllocationRuleDetail
            {
                DetailId = Guid.NewGuid(),
                UserId = d.UserId,
                AllocationRatio = d.AllocationRatio,
                RoleType = d.RoleType,
                AllocatedAmount = Math.Round(policy.CommissionAmount * d.AllocationRatio, 2)
            }).ToList()
        };

        if (currentActiveRule != null)
        {
            currentActiveRule.EffectiveEndDate = dto.EffectiveFromDate.AddDays(-1);
            currentActiveRule.IsActive = false;
        }

        _context.AllocationRules.Add(newRule);

        var adjustment = new AllocationAdjustment
        {
            AdjustmentId = Guid.NewGuid(),
            PolicyId = dto.PolicyId,
            OldRuleId = currentActiveRule?.RuleId,
            NewRuleId = newRule.RuleId,
            AdjustedByUserId = adjustedByUserId,
            AdjustmentReason = dto.AdjustmentReason,
            EffectiveFromDate = dto.EffectiveFromDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.AllocationAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        var result = await _context.AllocationAdjustments
            .Include(a => a.Policy)
            .Include(a => a.OldRule)
                .ThenInclude(r => r!.Details)
                .ThenInclude(d => d.User)
            .Include(a => a.NewRule)
                .ThenInclude(r => r.Details)
                .ThenInclude(d => d.User)
            .Include(a => a.NewRule)
                .ThenInclude(r => r.Details)
            .FirstAsync(a => a.AdjustmentId == adjustment.AdjustmentId, cancellationToken);

        return MapAdjustmentToDto(result);
    }

    public async Task<AllocationRuleDto?> GetRuleForMonthAsync(Guid policyId, DateTime settlementMonth, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(settlementMonth.Year, settlementMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var rule = await _context.AllocationRules
            .Include(r => r.Details)
            .ThenInclude(d => d.User)
            .Include(r => r.Policy)
            .Where(r => r.PolicyId == policyId
                && r.EffectiveStartDate <= monthEnd
                && (!r.EffectiveEndDate.HasValue || r.EffectiveEndDate.Value >= monthStart))
            .OrderByDescending(r => r.EffectiveStartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return rule == null ? null : MapToDto(rule);
    }

    private static AllocationRuleDto MapToDto(AllocationRule rule) => new(
        rule.RuleId,
        rule.PolicyId,
        rule.Policy?.PolicyNo ?? string.Empty,
        rule.EffectiveStartDate,
        rule.EffectiveEndDate,
        rule.IsActive,
        rule.Details.Select(MapDetailToDto).ToList()
    );

    private static AllocationRuleDetailDto MapDetailToDto(AllocationRuleDetail detail) => new(
        detail.DetailId,
        detail.UserId,
        detail.User?.UserName ?? string.Empty,
        detail.User?.UserCode ?? string.Empty,
        detail.AllocationRatio,
        detail.RoleType,
        detail.AllocatedAmount
    );

    private static AllocationAdjustmentDto MapAdjustmentToDto(AllocationAdjustment adj) => new(
        adj.AdjustmentId,
        adj.PolicyId,
        adj.Policy?.PolicyNo ?? string.Empty,
        adj.AdjustmentReason,
        adj.EffectiveFromDate,
        adj.CreatedAt,
        adj.AdjustedByUserId,
        string.Empty,
        adj.OldRule?.Details.Select(MapDetailToDto).ToList(),
        adj.NewRule.Details.Select(MapDetailToDto).ToList()
    );
}
