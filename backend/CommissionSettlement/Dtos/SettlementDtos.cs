using CommissionSettlement.Enums;

namespace CommissionSettlement.Dtos;

public record MonthlySettlementDto(
    Guid SettlementId,
    string SettlementMonth,
    Guid UserId,
    string UserName,
    string UserCode,
    decimal TotalCommission,
    decimal TotalClawback,
    decimal TotalPreTaxDeduction,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal NetPayable,
    SettlementStatus Status,
    DateTime GeneratedAt,
    DateTime? ApprovedAt,
    DateTime? PaidAt,
    List<SettlementSnapshotDto>? Snapshots
);

public record SettlementSnapshotDto(
    Guid SnapshotId,
    Guid SettlementId,
    Guid PolicyId,
    string PolicyNo,
    string PolicyStatus,
    decimal Premium,
    decimal CommissionRate,
    decimal OriginalCommission,
    decimal AllocationRatio,
    decimal AllocatedCommission,
    decimal ClawbackAmount,
    decimal PreTaxDeduction,
    string? RuleSnapshot,
    DateTime CreatedAt
);

public record GenerateSettlementDto(
    string SettlementMonth
);

public record SettlementQueryDto : PagedQueryDto
{
    public string? SettlementMonth { get; set; }
    public Guid? UserId { get; set; }
    public SettlementStatus? Status { get; set; }
}

public record ClawbackRecordDto(
    Guid ClawbackId,
    Guid PolicyId,
    string PolicyNo,
    ClawbackType ClawbackType,
    decimal ClawbackAmount,
    DateTime CancelledAt,
    string? Reason,
    bool IsCoolingPeriod,
    DateTime CreatedAt
);

public record PreTaxDeductionDto(
    Guid DeductionId,
    Guid UserId,
    string UserName,
    string DeductionType,
    decimal DeductionAmount,
    string DeductionMonth,
    string? Description,
    DateTime CreatedAt
);

public record CreatePreTaxDeductionDto(
    Guid UserId,
    string DeductionType,
    decimal DeductionAmount,
    string DeductionMonth,
    string? Description
);
