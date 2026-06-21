namespace CommissionSettlement.Models;

public class AllocationRule
{
    public Guid RuleId { get; set; }
    public Guid PolicyId { get; set; }
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Policy Policy { get; set; } = null!;
    public ICollection<AllocationRuleDetail> Details { get; set; } = new List<AllocationRuleDetail>();
    public ICollection<AllocationAdjustment> OldAdjustments { get; set; } = new List<AllocationAdjustment>();
    public ICollection<AllocationAdjustment> NewAdjustments { get; set; } = new List<AllocationAdjustment>();
}

public class AllocationRuleDetail
{
    public Guid DetailId { get; set; }
    public Guid RuleId { get; set; }
    public Guid UserId { get; set; }
    public decimal AllocationRatio { get; set; }
    public string RoleType { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }

    public AllocationRule Rule { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class AllocationAdjustment
{
    public Guid AdjustmentId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? OldRuleId { get; set; }
    public Guid NewRuleId { get; set; }
    public Guid AdjustedByUserId { get; set; }
    public string AdjustmentReason { get; set; } = string.Empty;
    public DateTime EffectiveFromDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Policy Policy { get; set; } = null!;
    public AllocationRule? OldRule { get; set; }
    public AllocationRule NewRule { get; set; } = null!;
}
