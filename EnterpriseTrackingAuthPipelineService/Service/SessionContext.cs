using EnterpriseTrackingAuthPipelineService.Dto;
using EnterpriseTrackingAuthPipelineService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTrackingAuthPipelineService.Service
{
    public sealed class SessionContext : ISessionContext
    {
        private readonly object _lock = new();
        private AuthResultDto? _current;

        public void SetCurrent(AuthResultDto session)
        {
            lock (_lock)
            {
                _current = session;
            }
        }

        public AuthResultDto? GetCurrent()
        {
            lock (_lock)
            {
                return _current;
            }
        }

        public string? CurrentUserId
        {
            get
            {
                lock (_lock)
                {
                    return _current?.UserId;
                }
            }
        }

        public string? CurrentUsername
        {
            get
            {
                lock (_lock)
                {
                    return _current?.Username;
                }
            }
        }
    }
}
