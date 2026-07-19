using EnterpriseTrackingAuthPipelineService.EnterpriseTrackingActivityAgent;

namespace EnterpriseTrackingAuthPipelineService
{

    public class AuthWindowsService
    {
        private readonly NamedPipeAuthHost _pipeHost;

        public AuthWindowsService(NamedPipeAuthHost pipeHost)
        {
            _pipeHost = pipeHost;
        }

        public void Start()
        {
            _pipeHost.Start();
            Console.WriteLine("Auth service started");
        }

        public void Stop()
        {
            _pipeHost.Stop();
            Console.WriteLine("Auth service stopped");
        }
    }
}
