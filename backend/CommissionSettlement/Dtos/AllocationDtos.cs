namespace CommissionSettlement.Dtos;

public record AllocationRuleDto(
    Guid RuleId,
    Guid PolicyId,
    string PolicyNo,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate,
    bool IsActive,
    List<AllocationRuleDetailDto> Details
);

public record AllocationRuleDetailDto(
    Guid DetailId,
    Guid UserId,
    string UserName,
    string UserCode,
    decimal AllocationRatio,
    string RoleType,
    decimal AllocatedAmount
);

public record CreateAllocationRuleDto(
    Guid PolicyId,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate,
    List<CreateAllocationRuleDetailDto> Details,
    string? AdjustmentReason
);

public record CreateAllocationRuleDetailDto(
    Guid UserId,
    decimal AllocationRatio,
    string RoleType
);

public record AdjustAllocationDto(
    Guid PolicyId,
    string AdjustmentReason,
    DateTime EffectiveFromDate,
    List<CreateAllocationRuleDetailDto> NewDetails
);

public record AllocationAdjustmentDto(
    Guid AdjustmentId,
    Guid PolicyId,
    string PolicyNo,
    string AdjustmentReason,
    DateTime EffectiveFromDate,
    DateTime CreatedAt,
    Guid AdjustedByUserId,
    string AdjustedByUserName,
    List<AllocationRuleDetailDto>? OldDetails,
    List<AllocationRuleDetailDto> NewDetails
);
