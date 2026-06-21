using CommissionSettlement.Enums;

namespace CommissionSettlement.Dtos;

public record PolicyDto(
    Guid PolicyId,
    string PolicyNo,
    string ProductName,
    string PolicyHolder,
    string Insured,
    decimal Premium,
    decimal CommissionRate,
    decimal CommissionAmount,
    PolicyStatus Status,
    Guid AgentUserId,
    string? AgentUserName,
    DateTime? SignedAt,
    DateTime? EffectiveAt,
    DateTime? CoolingPeriodEndAt,
    DateTime? CancelledAt,
    CancellationType? CancellationType
);

public record CreatePolicyDto(
    string PolicyNo,
    string ProductName,
    string PolicyHolder,
    string Insured,
    decimal Premium,
    decimal? CommissionRate,
    Guid AgentUserId
);

public record UpdatePolicyDto(
    string ProductName,
    string PolicyHolder,
    string Insured,
    decimal Premium,
    decimal CommissionRate
);

public record PolicyQueryDto : PagedQueryDto
{
    public string? PolicyNo { get; set; }
    public PolicyStatus? Status { get; set; }
    public Guid? AgentUserId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public record PagedQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
}
