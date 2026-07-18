using EnterpriseTrackingAuthPipelineService;
using EnterpriseTrackingAuthPipelineService.Cache;
using EnterpriseTrackingAuthPipelineService.EnterpriseTrackingActivityAgent;
using EnterpriseTrackingAuthPipelineService.Interface;
using EnterpriseTrackingAuthPipelineService.Service;
using Microsoft.Extensions.DependencyInjection;
using Topshelf;



// 🧱 DI
var services = new ServiceCollection();



// Existing dependencies (keep yours if any)
services.AddSingleton<IEncryptionService, EncryptionService>();
services.AddSingleton<ISessionContext, SessionContext>();
services.AddSingleton<AuthCacheStore>();
services.AddSingleton<AuthApiClient>();
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<NamedPipeAuthHost>();


services.AddSingleton<AuthWindowsService>();    // new

var provider = services.BuildServiceProvider();

HostFactory.Run(x =>
{

    // 🔹 NEW AUTH SERVICE
    x.Service<AuthWindowsService>(s =>
    {
        s.ConstructUsing(name => provider.GetRequiredService<AuthWindowsService>());
        s.WhenStarted(tc => tc.Start());
        s.WhenStopped(tc => tc.Stop());
    });

    x.RunAsLocalSystem();

    x.SetServiceName("AgentAuth");
    x.SetDisplayName("Gomtech named pipeline Agent");
    x.SetDescription("authentication pipe");

    x.StartAutomatically();
});