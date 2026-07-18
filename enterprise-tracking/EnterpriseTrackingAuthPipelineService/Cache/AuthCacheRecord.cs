using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTrackingAuthPipelineService.Cache
{
    public sealed class AuthCacheRecord
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public string EncryptedUserId { get; set; } = "";
        public string EncryptedJwt { get; set; } = "";
        public string EncryptedRoles { get; set; } = "";
        public string LastLoginUtc { get; set; } = "";
    }
}
