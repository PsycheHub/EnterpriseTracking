namespace EnterpriseTracking.Core.Entities
{
    public class PaymentTransaction : BaseEntity
    {
        public string CompanyId { get; set; } = string.Empty;
        public Company Company { get; set; } = null!;
        public string Reference { get; set; } = string.Empty;
        public long AmountKobo { get; set; }
        public string Currency { get; set; } = "NGN";
        public string Status { get; set; } = "Pending";
        public int Seats { get; set; }
        public string Plan { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }
}