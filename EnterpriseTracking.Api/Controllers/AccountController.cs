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
    /// <summary>User account management — registration, login, roles, and exports.</summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/account")]
    public class AccountController : BaseController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>Invite a new user by email. Sends an invitation email with setup instructions.</summary>
        [HttpPost("user/invite")]
        public async Task<IActionResult> RegisterUser(SignUp req)
        {
            var result = await _accountService.RegisterUser(req, "User");
            return HandleResponse(result);
        }

        /// <summary>Authenticate an admin or user and receive a JWT.</summary>
        [AllowAnonymous]
        [HttpPost("user/login")]
        public async Task<IActionResult> LoginUser(SignInModel req)
        {
            var result = await _accountService.LoginUser(req);
            return HandleResponse(result);
        }

        /// <summary>Authenticate via the desktop agent (voucher-based login).</summary>
        [AllowAnonymous]
        [HttpPost("agent/user/login")]
        public async Task<IActionResult> AgentLoginUser(AgentSignInUserReq req)
        {
            var result = await _accountService.LoginAgentUser(req);
            return HandleResponse(result);
        }

        /// <summary>Trigger a password reset email for the given user account.</summary>
        [HttpPost("user/reset_password")]
        public async Task<IActionResult> ResetPasswordAysnc(string email)
        {
            var result = await _accountService.ResetPassword(email);
            return HandleResponse(result);
        }

        /// <summary>Suspend a user account — the user will be blocked from logging in.</summary>
        [HttpPost("user/suspend")]
        public async Task<IActionResult> SuspendUser(string email)
        {
            var result = await _accountService.SuspendUserAsync(email);
            return HandleResponse(result);
        }

        /// <summary>Lift a suspension and restore access for the given user.</summary>
        [HttpPost("user/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(string email)
        {
            var result = await _accountService.UnSuspendUserAsync(email);
            return HandleResponse(result);
        }

        /// <summary>Change the role of a user (e.g. User → Admin).</summary>
        [HttpPost("user/role_change")]
        public async Task<IActionResult> UserRoleUpdateDto(UserRoleUpdateDto req)
        {
            var result = await _accountService.UpdateUserRole(req.Id, req.Role);
            return HandleResponse(result);
        }

        /// <summary>Export user records to an Excel (.xlsx) file.</summary>
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

        /// <summary>Return the profile of the currently authenticated admin.</summary>
        [HttpGet("user/admin/info")]
        public async Task<IActionResult> UserInfoAsync()
        {
            var userid = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var result = await _accountService.UserInfoAsync(userid);
            return HandleResponse(result);
        }

        /// <summary>Return aggregate user growth metrics (total, active, suspended, invited).</summary>
        [Authorize(Policy = "UsersRead")]
        [HttpGet("user/metrics")]
        public async Task<IActionResult> GetUserGrowthMetrics()
        {
            var result = await _accountService.GetUserGrowthMetricsAsync();
            return HandleResponse(result);
        }

        /// <summary>Return the full profile of a single user by their ID.</summary>
        [Authorize(Policy = "UsersRead")]
        [HttpGet("user/info")]
        public async Task<IActionResult> GetUserbyIdAsync(string Id)
        {
            var result = await _accountService.GetUserbyId(Id);
            return HandleResponse(result);
        }

        /// <summary>Return a paginated list of all users. Supports filtering by name/email, status, and voucher linkage.</summary>
        [Authorize(Policy = "UsersRead")]
        [HttpGet("user/all")]
        public async Task<IActionResult> GetPaginatedUser(int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked)
        {
            var result = await _accountService.GetPaginatedUserInfo(pageNumber, perPageSize,
                nameOrEmail, status, isVoucherLinked);
            return HandleResponse(result);
        }

        /// <summary>Permanently delete a user account (soft delete).</summary>
        [HttpDelete("user/delete")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var result = await _accountService.DeleteUser(email);
            return HandleResponse(result);
        }

    }
}
