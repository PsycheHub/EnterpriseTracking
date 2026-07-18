namespace EnterpriseTracking.Core.Entities
{
    public class Attendance : BaseEntity
    {
        public string UserId { get; set; }        // tie to user identity
        public ApplicationUser User { get; set; }
        public DateTime? FirstLoginUtc { get; set; } = DateTime.UtcNow;


    }
}
