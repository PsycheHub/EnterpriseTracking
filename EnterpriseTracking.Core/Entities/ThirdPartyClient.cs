namespace EnterpriseTracking.Core.Entities
{
    public class ThirdPartyClient : BaseEntity
    {
        public string CompanyId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecretHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        /// <summary>Comma-separated scopes: attendance,activity,users,vouchers</summary>
        public string Permissions { get; set; } = "attendance,activity";
        public DateTime? LastUsed { get; set; }
        public string CreatedByAdminId { get; set; } = string.Empty;
    }
}
