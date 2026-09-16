namespace EnterpriseTracking.Core.Dto.Response.Billing
{
    public class PaymentInitializationDto
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}