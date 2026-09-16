using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Repository.Interface;
using EnterpriseTracking.Infrastructure.Context;
using EnterpriseTracking.Infrastructure.Repository.Implementation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace EnterpriseTracking.Api.Seeder
{
    public class Seeder
    {
        public static async Task SeedData(IApplicationBuilder app)
        {

            // Get db context
            var dbContext = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<EnterpriseTrackingContext>();

            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
            }

            var roleManager = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            List<string> roles = new() { "SuperAdmin", "CompanyAdmin", "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            const string platformEmail = "superadmin@ogavix.com";
            var platformCompany = await dbContext.Set<Company>().FirstOrDefaultAsync(x => x.Email == platformEmail);
            if (platformCompany == null)
            {
                platformCompany = new Company { Name = "OGAVIX Platform", Email = platformEmail };
                dbContext.Set<Company>().Add(platformCompany);
                await dbContext.SaveChangesAsync();
            }

            var userManager = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var superAdmin = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == platformEmail);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    Email = platformEmail,
                    UserName = platformEmail,
                    FirstName = "OGAVIX",
                    LastName = "Super Admin",
                    CompanyId = platformCompany.Id,
                    Status = "Active"
                };
                var configuredPassword = Environment.GetEnvironmentVariable("OGAVIX_SUPERADMIN_PASSWORD") ?? "ChangeMe@2026!";
                var createUser = await userManager.CreateAsync(superAdmin, configuredPassword);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }
            await dbContext.SaveChangesAsync();
        }


    }
}
