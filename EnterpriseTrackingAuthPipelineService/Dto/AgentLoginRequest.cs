using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EnterpriseTrackingAuthPipelineService.Dto
{
    public sealed class AgentLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Voucher { get; set; } = "";
    }

    public sealed class AgentLoginApiResponse
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("displayMessage")]
        public string? DisplayMessage { get; set; }

        [JsonPropertyName("result")]
        public AgentLoginResult? Result { get; set; }

        [JsonPropertyName("errorMessages")]
        public List<string>? ErrorMessages { get; set; }
    }

    public sealed class AgentLoginResult
    {
        [JsonPropertyName("jwt")]
        public string? Jwt { get; set; }

        [JsonPropertyName("userRole")]
        public List<string>? UserRole { get; set; }
    }

    public sealed class AuthResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = ""; // online / offline
        public string? Username { get; set; }
        public string? UserId { get; set; }
        public string? Jwt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public sealed class PipeAuthRequest
    {
        public string Action { get; set; } = "login"; // login | get_session
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Voucher { get; set; } = "";
    }

    public sealed class PipeAuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";
        public string? Username { get; set; }
        public string? UserId { get; set; }
        public string? Jwt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

}
