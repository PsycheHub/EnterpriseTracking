using EnterpriseTracking.Core.Dto.Request.Integration;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Integration;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IThirdPartyClientService
    {
        Task<ResponseDto<CreatedClientDto>> CreateClientAsync(CreateThirdPartyClientReq req, string adminId);
        Task<ResponseDto<List<ThirdPartyClientDto>>> ListClientsAsync();
        Task<ResponseDto<string>> RevokeClientAsync(string id);
        Task<ResponseDto<CreatedClientDto>> RegenerateSecretAsync(string id);
        Task<ResponseDto<ClientTokenDto>> ExchangeTokenAsync(ClientTokenRequest req);
    }
}
