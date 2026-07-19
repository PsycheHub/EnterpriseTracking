using EnterpriseTracking.Core.Enum;
using Microsoft.AspNetCore.Identity;

namespace EnterpriseTracking.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {


        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; } = UserStatus.Invited.ToString();
        public DateTime? LastLoginTime { get; set; }
        public bool IsVoucherLinked { get; set; } = false;
        public bool IsSuspend { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeleteDate { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public Voucher Voucher { get; set; }
        public IEnumerable<Attendance> Attendances { get; set; }
        public IEnumerable<UserActivity> UserActivities { get; set; }
    }
}
