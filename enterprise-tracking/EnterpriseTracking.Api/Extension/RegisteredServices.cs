using EnterpriseTracking.Core.Helper;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using EnterpriseTracking.Infrastructure.OtherService.Implementation;
using EnterpriseTracking.Infrastructure.Repository.Implementation;

namespace ZedSystem.Api.Extension
{
    public static class RegisteredServices
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAccountRepo, AccountRepo>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped(typeof(IEnterpriseTrackingGenericRepo<>), typeof(EnterpriseTrackingGenericRepo<>));
            services.AddScoped<IGenerateJwt, GenerateJwt>();
           
            services.AddScoped<IEmailServices, EmailService>();
            services.AddScoped<IVoucherService, VoucherService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IHelperServ, HelperServ>();
            services.AddSingleton<IEmailServiceViaGmail, EmailServiceViaGmail>();
            services.AddScoped<IUserActivityService, UserActivityService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
           

        }
    }
}
