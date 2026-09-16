namespace EnterpriseTracking.Core.Entities
{
    public class CompanySubscription : BaseEntity
    {
        public string CompanyId { get; set; } = string.Empty;
        public Company Company { get; set; } = null!;
        public string Plan { get; set; } = "Trial";
        public string BillingCycle { get; set; } = "Monthly";
        public int SeatLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? CurrentPeriodEndsAt { get; set; }
        public string? PaystackCustomerCode { get; set; }
        public string? PaystackSubscriptionCode { get; set; }
    }
}