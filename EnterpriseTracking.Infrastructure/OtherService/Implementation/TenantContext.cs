using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
        public string? CompanyId => User?.FindFirst("company_id")?.Value;
        public string? UserId => User?.FindFirst(ClaimTypes.UserData)?.Value;
        public bool IsSuperAdmin => User?.IsInRole("SuperAdmin") == true;
    }
}