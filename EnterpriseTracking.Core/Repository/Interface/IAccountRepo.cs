using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Dto.Response.Auth;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;

namespace EnterpriseTracking.Core.Repository.Interface
{
    public interface IAccountRepo
    {
        Task<ApplicationUser?> FindUserByuSERNAMEAsync(string userName);
        Task<ApplicationUser> FindUserByIdFullinfoAsync(string id);
        Task<ApplicationUser> SignUpAsync(ApplicationUser user, string Password);

        Task<bool> CheckAccountPassword(ApplicationUser user, string password);


        Task<UserGrowthMetricsDto> GetUserGrowthMetricsAsync();




        Task<bool> AddRoleAsync(ApplicationUser user, string Role);

        Task<string> ForgotPassword(ApplicationUser user);

    

        Task<bool> RemoveRoleAsync(ApplicationUser user, IList<string> role);

        Task<bool> ResetPasswordAsync(ApplicationUser user, ResetPassword resetPassword);

        Task<bool> RoleExist(string Role);

        Task<ApplicationUser?> FindUserByEmailAsync(string email);

        Task<ApplicationUser> FindUserByIdAsync(string id);

        Task<bool> UpdateUserInfo(ApplicationUser applicationUser);

        Task<IList<string>> GetUserRoles(ApplicationUser user);
        Task<PaginatedUser> GetPaginatedUserInfoAsync(
    int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked);
        Task<byte[]> ExportUsersAsync(ExportUserRequest request);
        Task<DisplayFindUserDTO> FindUserByIdSingleAsync(string id);


    }
}
