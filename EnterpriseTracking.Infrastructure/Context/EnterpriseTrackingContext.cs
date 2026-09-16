using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTracking.Infrastructure.Context
{
    public class EnterpriseTrackingContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ITenantContext _tenantContext;
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<ProofOfActivity> ProofOfActivitys { get; set; }
        public DbSet<ActivityCategory> ActivityCategorys { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<MapAppCatories> MapAppCatories { get; set; }
        public DbSet<UserActivity> UserActivity { get; set; }
        public DbSet<AgentSession> AgentSessions { get; set; }
        public DbSet<ThirdPartyClient> ThirdPartyClients { get; set; }
        public EnterpriseTrackingContext(DbContextOptions options, ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Company>().HasIndex(x => x.Email).IsUnique();
            builder.Entity<Company>()
                .HasOne(x => x.Subscription)
                .WithOne(x => x.Company)
                .HasForeignKey<CompanySubscription>(x => x.CompanyId);
            builder.Entity<ApplicationUser>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId);
            builder.Entity<Voucher>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Vouchers)
                .HasForeignKey(x => x.CompanyId);
            builder.Entity<PaymentTransaction>()
                .HasIndex(x => x.Reference)
                .IsUnique();
            builder.Entity<PaymentTransaction>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.CompanyId);

            builder.Entity<ApplicationUser>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.CompanyId == _tenantContext.CompanyId);
            builder.Entity<Voucher>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.CompanyId == _tenantContext.CompanyId);
            builder.Entity<Attendance>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.User.CompanyId == _tenantContext.CompanyId);
            builder.Entity<UserActivity>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.User.CompanyId == _tenantContext.CompanyId);
            builder.Entity<ProofOfActivity>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.User.CompanyId == _tenantContext.CompanyId);
            builder.Entity<AgentSession>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.User.CompanyId == _tenantContext.CompanyId);
            builder.Entity<ThirdPartyClient>().HasQueryFilter(x =>
                _tenantContext.IsSuperAdmin || x.CompanyId == _tenantContext.CompanyId);
        }
    }
}
