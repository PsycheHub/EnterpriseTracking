namespace EnterpriseTracking.Core.Dto.Request.Integration
{
    public class CreateThirdPartyClientReq
    {
        public string AppName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        /// <summary>Comma-separated: attendance,activity,users,vouchers</summary>
        public string Permissions { get; set; } = "attendance,activity";
    }

    public class ClientTokenRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
