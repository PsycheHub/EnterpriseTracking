namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class overallUserDataResp
    {
        public string TotalTimeSpent { get; set; }
        public List<ChartUserActivitiesOverall> ChartData { get; set; }
    }
    public class ChartUserActivitiesOverall
    {
        public int No { get; set; }
        public string AppName { get; set; }
        public string Category { get; set; }
        public string TimeSpent { get; set; }
        public double PercentageUsage { get; set; }
    }
}
