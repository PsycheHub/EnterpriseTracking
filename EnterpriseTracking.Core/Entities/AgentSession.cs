using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseTracking.Core.Entities
{
    public class AgentSession : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime ExpiresAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }
}
