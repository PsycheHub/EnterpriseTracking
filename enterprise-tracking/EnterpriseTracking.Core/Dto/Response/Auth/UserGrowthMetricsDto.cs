namespace EnterpriseTracking.Core.Dto.Response.Auth
{
    public class UserGrowthMetricsDto
    {
        public int TotalUsers { get; set; }
      
        public int ActiveUsers { get; set; }
    

        public int SuspendedUsers { get; set; }
     

        public int InvitedUsers { get; set; }
        
    }
}
