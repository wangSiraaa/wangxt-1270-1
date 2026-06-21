using CommissionSettlement.Enums;

namespace CommissionSettlement.Models;

public class Policy
{
    public Guid PolicyId { get; set; }
    public string PolicyNo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string PolicyHolder { get; set; } = string.Empty;
    public string Insured { get; set; } = string.Empty;
    public decimal Premium { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public PolicyStatus Status { get; set; }
    public Guid AgentUserId { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? EffectiveAt { get; set; }
    public DateTime? CoolingPeriodEndAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public CancellationType? CancellationType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }

    public User AgentUser { get; set; } = null!;
    public ICollection<AllocationRule> AllocationRules { get; set; } = new List<AllocationRule>();
    public ICollection<AllocationAdjustment> AllocationAdjustments { get; set; } = new List<AllocationAdjustment>();
    public ICollection<SettlementSnapshot> SettlementSnapshots { get; set; } = new List<SettlementSnapshot>();
    public ICollection<ClawbackRecord> ClawbackRecords { get; set; } = new List<ClawbackRecord>();
}
