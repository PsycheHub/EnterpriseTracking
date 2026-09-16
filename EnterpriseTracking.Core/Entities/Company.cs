namespace EnterpriseTracking.Core.Entities
{
    public class Company : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? RcNumber { get; set; }
        public bool IsSuspended { get; set; }
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
        public ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
        public CompanySubscription? Subscription { get; set; }
    }
}