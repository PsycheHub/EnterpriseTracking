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

            if (!dbContext.Roles.Any())
            {
                await dbContext.Database.EnsureCreatedAsync();
                var roleManager = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                List<string> roles = new() { "Admin", "User" };
                foreach (var role in roles)
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role });
                }

            }
            if (!dbContext.Users.Any())
            {
                var getAccountRepo = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                
                var mapAccount = new ApplicationUser();

                mapAccount.Email = "info@gomlearning.app.com";
                mapAccount.FirstName = "gomlearning";
                mapAccount.LastName = "gomlearning";
                mapAccount.UserName = "info@gomlearning.app.com";


                var generatePassowrd = "Gomlearning@1";
                var createUser = await getAccountRepo.CreateAsync(mapAccount, generatePassowrd);
                if (createUser != null)
                {
                    await getAccountRepo.AddToRoleAsync(mapAccount, "Admin");
                }
               



            }
            await dbContext.SaveChangesAsync();
        }


    }
}
