using CommissionSettlement.Enums;

namespace CommissionSettlement.Models;

public class MonthlySettlement
{
    public Guid SettlementId { get; set; }
    public string SettlementMonth { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalClawback { get; set; }
    public decimal TotalPreTaxDeduction { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal NetPayable { get; set; }
    public SettlementStatus Status { get; set; }
    public Guid GeneratedByUserId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<SettlementSnapshot> Snapshots { get; set; } = new List<SettlementSnapshot>();
}

public class SettlementSnapshot
{
    public Guid SnapshotId { get; set; }
    public Guid SettlementId { get; set; }
    public Guid PolicyId { get; set; }
    public string PolicyNo { get; set; } = string.Empty;
    public string PolicyStatus { get; set; } = string.Empty;
    public decimal Premium { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal OriginalCommission { get; set; }
    public decimal AllocationRatio { get; set; }
    public decimal AllocatedCommission { get; set; }
    public decimal ClawbackAmount { get; set; }
    public decimal PreTaxDeduction { get; set; }
    public string? RuleSnapshot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MonthlySettlement Settlement { get; set; } = null!;
    public Policy Policy { get; set; } = null!;
}

public class ClawbackRecord
{
    public Guid ClawbackId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? OriginalSettlementId { get; set; }
    public ClawbackType ClawbackType { get; set; }
    public decimal ClawbackAmount { get; set; }
    public DateTime CancelledAt { get; set; }
    public string? Reason { get; set; }
    public bool IsCoolingPeriod { get; set; }
    public Guid? AffectedSettlementId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Policy Policy { get; set; } = null!;
}

public class PreTaxDeduction
{
    public Guid DeductionId { get; set; }
    public Guid UserId { get; set; }
    public string DeductionType { get; set; } = string.Empty;
    public decimal DeductionAmount { get; set; }
    public string DeductionMonth { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
