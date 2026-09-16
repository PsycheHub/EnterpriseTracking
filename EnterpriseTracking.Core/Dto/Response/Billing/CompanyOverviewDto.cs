namespace EnterpriseTracking.Core.Dto.Response.Billing
{
    public class CompanyOverviewDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsSuspended { get; set; }
        public string Plan { get; set; } = string.Empty;
        public int SeatLimit { get; set; }
        public int UsedSeats { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public DateTime? CurrentPeriodEndsAt { get; set; }
        public long TotalPaidKobo { get; set; }
    }
}