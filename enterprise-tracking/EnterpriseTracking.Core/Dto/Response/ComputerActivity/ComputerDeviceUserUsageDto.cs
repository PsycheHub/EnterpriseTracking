namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ComputerDeviceUserUsageDto
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string HoursSpent { get; set; } = "0h 0m";
        public double PercentageUsage { get; set; }
    }
}