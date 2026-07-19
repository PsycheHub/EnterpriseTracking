using EnterpriseTrackingAuthPipelineService.Dto;

namespace EnterpriseTrackingAuthPipelineService.Interface
{
    public interface IAuthService
    {
        Task<AuthResultDto> ValidateAsync(AgentLoginRequest request, CancellationToken cancellationToken = default);
        Task<AuthResultDto?> GetCurrentSessionAsync();
    }
}
