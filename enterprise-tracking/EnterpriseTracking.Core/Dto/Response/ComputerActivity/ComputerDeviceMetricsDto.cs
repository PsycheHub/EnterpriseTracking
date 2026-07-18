namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ComputerDeviceMetricsDto
    {
        public int TotalComputerDevices { get; set; }
        public int TotalActiveDevices { get; set; }
        public string TotalHoursOfUsage { get; set; } = "0h 0m";
    }
}