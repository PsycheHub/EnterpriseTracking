namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ActivityInsightMetricsDto
    {
        public string AvgSessionDuration { get; set; } = "0s";
        public string PeakUsageTime { get; set; } = "N/A";
        public decimal WeeklyGrowth { get; set; }
    }
}