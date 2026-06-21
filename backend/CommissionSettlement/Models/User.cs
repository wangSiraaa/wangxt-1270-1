using CommissionSettlement.Enums;

namespace CommissionSettlement.Models;

public class User
{
    public Guid UserId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ParentUserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? ParentUser { get; set; }
    public ICollection<User> Subordinates { get; set; } = new List<User>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<AllocationRuleDetail> AllocationDetails { get; set; } = new List<AllocationRuleDetail>();
    public ICollection<MonthlySettlement> Settlements { get; set; } = new List<MonthlySettlement>();
    public ICollection<PreTaxDeduction> PreTaxDeductions { get; set; } = new List<PreTaxDeduction>();
}
