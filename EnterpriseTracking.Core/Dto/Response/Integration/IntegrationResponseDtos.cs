namespace EnterpriseTracking.Core.Dto.Response.Integration
{
    public class CreatedClientDto
    {
        public string Id { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        /// <summary>Shown only once. Store it securely.</summary>
        public string ClientSecret { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
        public DateTime Created { get; set; }
    }

    public class ThirdPartyClientDto
    {
        public string Id { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public DateTime? LastUsed { get; set; }
        public DateTime Created { get; set; }
    }

    public class ClientTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}
