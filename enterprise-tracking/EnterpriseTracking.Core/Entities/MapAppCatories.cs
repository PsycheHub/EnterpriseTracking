namespace EnterpriseTracking.Core.Entities
{
    public class MapAppCatories : BaseEntity
    {
        public string Name { get; set; }
        public string? IconUrl { get; set; }
        public string EventType { get; set; }
        public string CategoryId { get; set; }
        public ActivityCategory Category { get; set; }
        public List<UserActivity> userActivities { get; set; }
    }
}
