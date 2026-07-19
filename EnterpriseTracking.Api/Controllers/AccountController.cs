using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace EnterpriseTracking.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/account")]

    public class AccountController : BaseController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("user/invite")]
        public async Task<IActionResult> RegisterUser(SignUp req)
        {
            var result = await _accountService.RegisterUser(req, "User");
            return HandleResponse(result);
        }
        [AllowAnonymous]
        [HttpPost("user/login")]
        public async Task<IActionResult> LoginUser(SignInModel req)
        {
            var result = await _accountService.LoginUser(req);
            return HandleResponse(result);
        }
        [AllowAnonymous]
        [HttpPost("agent/user/login")]
        public async Task<IActionResult> AgentLoginUser(AgentSignInUserReq req)
        {
            var result = await _accountService.LoginAgentUser(req);
            return HandleResponse(result);
        }
        [HttpPost("user/reset_password")]
        public async Task<IActionResult> ResetPasswordAysnc(string email)
        {
            var result = await _accountService.ResetPassword(email);
            return HandleResponse(result);
        }
        [HttpPost("user/suspend")]
        public async Task<IActionResult> SuspendUser(string email)
        {
            var result = await _accountService.SuspendUserAsync(email);
            return HandleResponse(result);
        }
        [HttpPost("user/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(string email)
        {
            var result = await _accountService.UnSuspendUserAsync(email);
            return HandleResponse(result);
        }
        [HttpPost("user/role_change")]
        public async Task<IActionResult> UserRoleUpdateDto(UserRoleUpdateDto req)
        {
            var result = await _accountService.UpdateUserRole(req.Id, req.Role);
            return HandleResponse(result);
        }
        [HttpPost("user/export")]
        public async Task<IActionResult> ExportUserInfo(ExportUserRequest request)
        {
            var file = await _accountService.ExportUserInfo(request);
            return File(
                file.Result,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Users_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
            );
        }
        [HttpGet("user/admin/info")]
        public async Task<IActionResult> UserInfoAsync()
        {
            var userid = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var result = await _accountService.UserInfoAsync(userid);
            return HandleResponse(result);
        }
        [HttpGet("user/metrics")]
        public async Task<IActionResult> GetUserGrowthMetrics()
        {

            var result = await _accountService.GetUserGrowthMetricsAsync();
            return HandleResponse(result);
        }
        
        [HttpGet("user/info")]
        public async Task<IActionResult> GetUserbyIdAsync(string Id)
        {

            var result = await _accountService.GetUserbyId(Id);
            return HandleResponse(result);
        }
        [HttpGet("user/all")]
        public async Task<IActionResult> GetPaginatedUser(int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked)
        {

            var result = await _accountService.GetPaginatedUserInfo(pageNumber,perPageSize,
                nameOrEmail,status,isVoucherLinked);
            return HandleResponse(result);
        }
        [HttpDelete("user/delete")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var result = await _accountService.DeleteUser(email);
            return HandleResponse(result);
        }

    }
}
