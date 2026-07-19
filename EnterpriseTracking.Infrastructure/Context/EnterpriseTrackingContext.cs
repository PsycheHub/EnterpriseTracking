using EnterpriseTracking.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTracking.Infrastructure.Context
{
    public class EnterpriseTrackingContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<ProofOfActivity> ProofOfActivitys { get; set; }
        public DbSet<ActivityCategory> ActivityCategorys { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<MapAppCatories> MapAppCatories { get; set; }
        public DbSet<UserActivity> UserActivity { get; set; }
        public DbSet<AgentSession> AgentSessions { get; set; }
        public EnterpriseTrackingContext(DbContextOptions options) : base(options) { }
    }
}
