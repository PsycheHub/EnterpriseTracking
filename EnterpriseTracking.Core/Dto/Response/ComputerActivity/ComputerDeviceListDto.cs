namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ComputerDeviceListDto
    {
        public int Rank { get; set; }
        public string ComputerName { get; set; } = string.Empty;
        public string HoursSpent { get; set; } = "0h 0m";
        public int TotalUserCount { get; set; }
        public double PercentageUsage { get; set; }
    }
}