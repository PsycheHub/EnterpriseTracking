using EnterpriseTracking.Core.Entities;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IGenerateJwt
    {
        Task<string> GenerateToken(ApplicationUser user);
    }
}
