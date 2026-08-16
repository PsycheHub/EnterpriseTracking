using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Dto.Request.Integration;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace EnterpriseTracking.Api.Controllers
{
    /// <summary>
    /// Manages third-party API client credentials and token exchange.
    /// Super admins use this to issue Client ID / Client Secret pairs to external systems.
    /// Third-party systems use POST /token to obtain a short-lived Bearer token.
    /// </summary>
    [Route("api/integration")]
    [ApiController]
    public class IntegrationController : BaseController
    {
        private readonly IThirdPartyClientService _clientService;

        public IntegrationController(IThirdPartyClientService clientService)
        {
            _clientService = clientService;
        }

        /// <summary>
        /// [Super Admin] Register a new external application and receive its Client ID + Secret.
        /// The client secret is shown only once — store it immediately.
        /// </summary>
        /// <param name="req">Application name, contact email, and permitted scopes.</param>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpPost("client/create")]
        public async Task<IActionResult> CreateClient([FromBody] CreateThirdPartyClientReq req)
        {
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.UserData)?.Value ?? string.Empty;
            var result = await _clientService.CreateClientAsync(req, adminId);
            return HandleResponse(result);
        }

        /// <summary>
        /// [Super Admin] List all registered third-party applications.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpGet("client/list")]
        public async Task<IActionResult> ListClients()
        {
            var result = await _clientService.ListClientsAsync();
            return HandleResponse(result);
        }

        /// <summary>
        /// [Super Admin] Revoke (deactivate) a third-party client. The client will no longer
        /// be able to exchange tokens.
        /// </summary>
        /// <param name="id">The internal record ID of the client.</param>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpDelete("client/revoke")]
        public async Task<IActionResult> RevokeClient([FromQuery] string id)
        {
            var result = await _clientService.RevokeClientAsync(id);
            return HandleResponse(result);
        }

        /// <summary>
        /// [Super Admin] Regenerate the client secret for an existing application.
        /// The old secret is immediately invalidated. The new secret is shown only once.
        /// </summary>
        /// <param name="id">The internal record ID of the client.</param>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpPost("client/regenerate-secret")]
        public async Task<IActionResult> RegenerateSecret([FromQuery] string id)
        {
            var result = await _clientService.RegenerateSecretAsync(id);
            return HandleResponse(result);
        }

        /// <summary>
        /// [Public] Exchange a Client ID + Client Secret for a short-lived Bearer access token.
        /// The token grants read access to the scopes assigned to this client.
        /// Token lifetime: 3600 seconds (1 hour).
        /// </summary>
        /// <remarks>
        /// **Example request:**
        /// ```json
        /// {
        ///   "clientId": "ogvx_abc123...",
        ///   "clientSecret": "yourSecretHere"
        /// }
        /// ```
        /// **Example response:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "result": {
        ///     "accessToken": "eyJ...",
        ///     "expiresInSeconds": 3600,
        ///     "tokenType": "Bearer"
        ///   }
        /// }
        /// ```
        /// Use the returned token as: `Authorization: Bearer {accessToken}`
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> ExchangeToken([FromBody] ClientTokenRequest req)
        {
            var result = await _clientService.ExchangeTokenAsync(req);
            return HandleResponse(result);
        }
    }
}
