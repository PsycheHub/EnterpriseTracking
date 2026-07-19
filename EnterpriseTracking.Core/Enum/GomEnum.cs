namespace EnterpriseTracking.Core.Enum
{
    public enum UserStatus
    {
      
        Invited,
        Active,
        Suspended
    }
    public enum VoucherStatus
    {
       
        Linked=1,
        Active,
        Unlinked,
        Expired
    }
    public enum TrendRange
    {
        Last7Days=1,
        Last30Days,
        CurrentYear
    }
}
