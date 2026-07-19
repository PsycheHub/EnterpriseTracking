namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class DashboardMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveVouchers { get; set; }
        public int TotalAttendance { get; set; }
        public string TotalActivityTime { get; set; } = "0s";
    }
}