using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EnterpriseTrackingAuthPipelineService.Helper
{




    public static class JwtHelper
    {
        public static string? GetUserId(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // ✅ PRIMARY: ClaimTypes.UserData
            var userId = token.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.UserData ||
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata"
            )?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
                return userId;

            // 🔁 FALLBACK: JTI
            userId = token.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Jti
            )?.Value;

            return userId;
        }

        public static string? GetUsername(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            return token.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name
            )?.Value;
        }

        public static string? GetEmail(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            return token.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email
            )?.Value;
        }

        public static List<string> GetRoles(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            return token.Claims
                .Where(c =>
                    c.Type == ClaimTypes.Role ||
                    c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                )
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }

        public static bool IsExpired(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            if (token.ValidTo == DateTime.MinValue)
                return true;

            return token.ValidTo < DateTime.UtcNow;
        }

        public static DateTime GetExpiry(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            return token.ValidTo;
        }
    }
}

