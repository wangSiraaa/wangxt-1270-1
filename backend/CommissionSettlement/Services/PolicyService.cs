using CommissionSettlement.Common;
using CommissionSettlement.Data;
using CommissionSettlement.Dtos;
using CommissionSettlement.Enums;
using CommissionSettlement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommissionSettlement.Services;

public interface IPolicyService
{
    Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto dto, CancellationToken cancellationToken = default);
    Task<PolicyDto> UpdatePolicyAsync(Guid policyId, UpdatePolicyDto dto, CancellationToken cancellationToken = default);
    Task<PolicyDto> SignPolicyAsync(Guid policyId, DateTime signedAt, CancellationToken cancellationToken = default);
    Task<PolicyDto> MakeEffectiveAsync(Guid policyId, DateTime effectiveAt, CancellationToken cancellationToken = default);
    Task<PolicyDto> CancelPolicyAsync(Guid policyId, CancellationType cancellationType, string? reason, DateTime cancelledAt, CancellationToken cancellationToken = default);
    Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<PagedResult<PolicyDto>> QueryPoliciesAsync(PolicyQueryDto query, CancellationToken cancellationToken = default);
    Task<bool> CanCommissionAsync(Guid policyId, CancellationToken cancellationToken = default);
}

public class PolicyService : IPolicyService
{
    private readonly AppDbContext _context;
    private readonly BusinessRuleOptions _rules;

    public PolicyService(AppDbContext context, IOptions<BusinessRuleOptions> rules)
    {
        _context = context;
        _rules = rules.Value;
    }

    public async Task<PolicyDto> CreatePolicyAsync(CreatePolicyDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Policies.AnyAsync(p => p.PolicyNo == dto.PolicyNo, cancellationToken);
        if (existing)
            throw new InvalidOperationException($"保单号 {dto.PolicyNo} 已存在");

        var agent = await _context.Users.FindAsync(new object?[] { dto.AgentUserId }, cancellationToken);
        if (agent == null || agent.Role != UserRole.Agent)
            throw new InvalidOperationException("业务员不存在或角色不正确");

        var rate = dto.CommissionRate ?? _rules.DefaultCommissionRate;
        var policy = new Policy
        {
            PolicyId = Guid.NewGuid(),
            PolicyNo = dto.PolicyNo,
            ProductName = dto.ProductName,
            PolicyHolder = dto.PolicyHolder,
            Insured = dto.Insured,
            Premium = dto.Premium,
            CommissionRate = rate,
            CommissionAmount = Math.Round(dto.Premium * rate, 2),
            Status = PolicyStatus.Pending,
            AgentUserId = dto.AgentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Policies.Add(policy);

        var defaultRule = CreateDefaultAllocationRule(policy, dto.AgentUserId);
        _context.AllocationRules.Add(defaultRule);

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(policy, cancellationToken);
    }

    private AllocationRule CreateDefaultAllocationRule(Policy policy, Guid agentUserId)
    {
        return new AllocationRule
        {
            RuleId = Guid.NewGuid(),
            PolicyId = policy.PolicyId,
            EffectiveStartDate = DateTime.UtcNow.Date,
            IsActive = true,
            CreatedByUserId = agentUserId,
            CreatedAt = DateTime.UtcNow,
            Details = new List<AllocationRuleDetail>
            {
                new()
                {
                    DetailId = Guid.NewGuid(),
                    UserId = agentUserId,
                    AllocationRatio = 1.0m,
                    RoleType = AllocationRole.DirectAgent.ToString(),
                    AllocatedAmount = policy.CommissionAmount
                }
            }
        };
    }

    public async Task<PolicyDto> UpdatePolicyAsync(Guid policyId, UpdatePolicyDto dto, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies.FindAsync(new object?[] { policyId }, cancellationToken);
        if (policy == null)
            throw new InvalidOperationException("保单不存在");

        if (policy.Status >= PolicyStatus.Signed)
            throw new InvalidOperationException("已签署的保单不能修改核心信息");

        policy.ProductName = dto.ProductName;
        policy.PolicyHolder = dto.PolicyHolder;
        policy.Insured = dto.Insured;
        policy.Premium = dto.Premium;
        policy.CommissionRate = dto.CommissionRate;
        policy.CommissionAmount = Math.Round(dto.Premium * dto.CommissionRate, 2);
        policy.UpdatedAt = DateTime.UtcNow;

        await UpdateDefaultAllocationAmountAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(policy, cancellationToken);
    }

    private async Task UpdateDefaultAllocationAmountAsync(Policy policy, CancellationToken cancellationToken)
    {
        var activeRule = await _context.AllocationRules
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.PolicyId == policy.PolicyId && r.IsActive && r.EffectiveEndDate == null, cancellationToken);

        if (activeRule != null)
        {
            foreach (var detail in activeRule.Details)
            {
                detail.AllocatedAmount = Math.Round(policy.CommissionAmount * detail.AllocationRatio, 2);
            }
        }
    }

    public async Task<PolicyDto> SignPolicyAsync(Guid policyId, DateTime signedAt, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies.FindAsync(new object?[] { policyId }, cancellationToken);
        if (policy == null)
            throw new InvalidOperationException("保单不存在");

        if (policy.Status != PolicyStatus.Pending)
            throw new InvalidOperationException("只有待签署状态的保单可以签署");

        policy.Status = PolicyStatus.Signed;
        policy.SignedAt = signedAt;
        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(policy, cancellationToken);
    }

    public async Task<PolicyDto> MakeEffectiveAsync(Guid policyId, DateTime effectiveAt, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies.FindAsync(new object?[] { policyId }, cancellationToken);
        if (policy == null)
            throw new InvalidOperationException("保单不存在");

        if (policy.Status != PolicyStatus.Signed && policy.Status != PolicyStatus.Effective)
            throw new InvalidOperationException("只有已签署状态的保单可以生效");

        policy.Status = PolicyStatus.Effective;
        policy.EffectiveAt = effectiveAt;
        policy.CoolingPeriodEndAt = effectiveAt.AddDays(_rules.DefaultCoolingPeriodDays);
        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(policy, cancellationToken);
    }

    public async Task<PolicyDto> CancelPolicyAsync(Guid policyId, CancellationType cancellationType, string? reason, DateTime cancelledAt, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies
            .Include(p => p.SettlementSnapshots)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId, cancellationToken);

        if (policy == null)
            throw new InvalidOperationException("保单不存在");

        if (policy.Status == PolicyStatus.Cancelled || policy.Status == PolicyStatus.Surrendered)
            throw new InvalidOperationException("保单已取消/退保");

        var isCoolingPeriod = cancellationType == CancellationType.CoolingPeriod
            && policy.CoolingPeriodEndAt.HasValue
            && cancelledAt <= policy.CoolingPeriodEndAt.Value;

        policy.Status = cancellationType == CancellationType.CoolingPeriod ? PolicyStatus.Cancelled : PolicyStatus.Surrendered;
        policy.CancelledAt = cancelledAt;
        policy.CancellationType = cancellationType;
        policy.UpdatedAt = DateTime.UtcNow;

        await CreateClawbackRecordAsync(policy, cancellationType, reason, isCoolingPeriod, cancelledAt, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(policy, cancellationToken);
    }

    private async Task CreateClawbackRecordAsync(Policy policy, CancellationType cancellationType, string? reason, bool isCoolingPeriod, DateTime cancelledAt, CancellationToken cancellationToken)
    {
        var clawbackType = cancellationType == CancellationType.CoolingPeriod
            ? ClawbackType.CoolingPeriodCancel
            : cancellationType == CancellationType.NormalSurrender
                ? ClawbackType.NormalSurrender
                : ClawbackType.Other;

        decimal clawbackAmount;
        if (_rules.CoolingPeriodFullClawback && isCoolingPeriod)
        {
            clawbackAmount = policy.CommissionAmount;
        }
        else
        {
            clawbackAmount = Math.Round(policy.CommissionAmount * 0.5m, 2);
        }

        var lastSettlement = policy.SettlementSnapshots
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        var clawback = new ClawbackRecord
        {
            ClawbackId = Guid.NewGuid(),
            PolicyId = policy.PolicyId,
            OriginalSettlementId = lastSettlement?.SettlementId,
            ClawbackType = clawbackType,
            ClawbackAmount = clawbackAmount,
            CancelledAt = cancelledAt,
            Reason = reason,
            IsCoolingPeriod = isCoolingPeriod,
            CreatedAt = DateTime.UtcNow
        };

        _context.ClawbackRecords.Add(clawback);
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies
            .Include(p => p.AgentUser)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId, cancellationToken);

        return policy == null ? null : await MapToDtoAsync(policy, cancellationToken);
    }

    public async Task<PagedResult<PolicyDto>> QueryPoliciesAsync(PolicyQueryDto query, CancellationToken cancellationToken = default)
    {
        var q = _context.Policies.Include(p => p.AgentUser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.PolicyNo))
            q = q.Where(p => p.PolicyNo.Contains(query.PolicyNo!));
        if (query.Status.HasValue)
            q = q.Where(p => p.Status == query.Status.Value);
        if (query.AgentUserId.HasValue)
            q = q.Where(p => p.AgentUserId == query.AgentUserId.Value);
        if (query.EffectiveFrom.HasValue)
            q = q.Where(p => p.EffectiveAt >= query.EffectiveFrom.Value);
        if (query.EffectiveTo.HasValue)
            q = q.Where(p => p.EffectiveAt <= query.EffectiveTo.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return new PagedResult<PolicyDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> CanCommissionAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _context.Policies.FindAsync(new object?[] { policyId }, cancellationToken);
        if (policy == null) return false;

        return policy.Status == PolicyStatus.Effective
            && policy.EffectiveAt.HasValue
            && policy.EffectiveAt.Value <= DateTime.UtcNow;
    }

    private Task<PolicyDto> MapToDtoAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        var dto = MapToDto(policy);
        return Task.FromResult(dto);
    }

    private static PolicyDto MapToDto(Policy policy) => new(
        policy.PolicyId,
        policy.PolicyNo,
        policy.ProductName,
        policy.PolicyHolder,
        policy.Insured,
        policy.Premium,
        policy.CommissionRate,
        policy.CommissionAmount,
        policy.Status,
        policy.AgentUserId,
        policy.AgentUser?.UserName,
        policy.SignedAt,
        policy.EffectiveAt,
        policy.CoolingPeriodEndAt,
        policy.CancelledAt,
        policy.CancellationType
    );
}

public class BusinessRuleOptions
{
    public int DefaultCoolingPeriodDays { get; set; } = 15;
    public bool CoolingPeriodFullClawback { get; set; } = true;
    public decimal DefaultCommissionRate { get; set; } = 0.20m;
}
