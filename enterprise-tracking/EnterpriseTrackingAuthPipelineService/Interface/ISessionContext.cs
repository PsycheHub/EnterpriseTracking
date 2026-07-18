using EnterpriseTrackingAuthPipelineService.Dto;

namespace EnterpriseTrackingAuthPipelineService.Interface
{
    public interface ISessionContext
    {
        void SetCurrent(AuthResultDto session);
        AuthResultDto? GetCurrent();
        string? CurrentUserId { get; }
        string? CurrentUsername { get; }
    }
}
