using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace ZedSystem.Api.Extension
{
    public static class DbRegisteredExtension
    {
        public static void ConfigureDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;
                options.Password.RequiredUniqueChars = 0;
            })
  .AddEntityFrameworkStores<EnterpriseTrackingContext>()
  .AddDefaultTokenProviders();

            services.AddDbContext<EnterpriseTrackingContext>(dbContextOptions =>
            {
                var connectionString = configuration.GetConnectionString("ProdDB");
                var maxRetryCount = 3;
                var maxRetryDelay = TimeSpan.FromSeconds(10);

                dbContextOptions.UseNpgsql(connectionString, options =>
                {
                    options.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
                });
            });
        }
    }
}
