namespace CommissionSettlement.Enums;

public enum UserRole
{
    Agent,
    Supervisor,
    Finance,
    Admin
}

public enum PolicyStatus
{
    Draft,
    Pending,
    Signed,
    Effective,
    CoolingPeriod,
    Cancelled,
    Surrendered
}

public enum CancellationType
{
    CoolingPeriod,
    NormalSurrender,
    Other
}

public enum SettlementStatus
{
    Draft,
    Generated,
    Approved,
    Paid,
    Rejected
}

public enum ClawbackType
{
    CoolingPeriodCancel,
    NormalSurrender,
    Adjustment,
    Other
}

public enum AllocationRole
{
    DirectAgent,
    TeamLeader,
    DepartmentHead,
    Other
}
