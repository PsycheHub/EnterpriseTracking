namespace EnterpriseTracking.Core.Dto.Response.Auth
{
    public class LoginResultDto
    {
        public string Jwt { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public IList<string>? UserRole { get; set; }
    }
}
