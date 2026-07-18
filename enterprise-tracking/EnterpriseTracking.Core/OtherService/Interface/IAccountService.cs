using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Auth;
using EnterpriseTracking.Core.Enum;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IAccountService
    {

        Task<ResponseDto<string>> ResetPassword(string email);

        Task<ResponseDto<LoginResultDto>> LoginAgentUser(AgentSignInUserReq signIn);
        Task<ResponseDto<string>> DeleteUser(string email);
        Task<ResponseDto<string>> RegisterUser(SignUp signUp, string Role);
        Task<ResponseDto<LoginResultDto>> LoginUser(SignInModel signIn);

        Task<ResponseDto<UserInfo>> UserInfoAsync(string userId);
        Task<ResponseDto<UserGrowthMetricsDto>> GetUserGrowthMetricsAsync();
        Task<ResponseDto<DisplayFindUserDTO>> GetUserbyId(string userId);
        Task<ResponseDto<string>> SuspendUserAsync(string useremail);
        Task<ResponseDto<string>> UnSuspendUserAsync(string useremail);

        Task<ResponseDto<string>> UpdateUserRole(string id, string role);
        Task<ResponseDto<byte[]>> ExportUserInfo(ExportUserRequest request);
        Task<ResponseDto<PaginatedUser>> GetPaginatedUserInfo(int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked);


    }
}
