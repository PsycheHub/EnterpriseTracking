namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface ITenantContext
    {
        string? CompanyId { get; }
        string? UserId { get; }
        bool IsSuperAdmin { get; }
    }
}