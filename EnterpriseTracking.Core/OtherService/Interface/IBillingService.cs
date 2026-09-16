using EnterpriseTracking.Core.Dto.Request.Billing;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Billing;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IBillingService
    {
        Task<ResponseDto<PaymentInitializationDto>> InitializePayment(InitializePaymentDto request);
        Task<ResponseDto<string>> VerifyPayment(string reference);
        Task<ResponseDto<string>> ProcessWebhook(string payload, string signature);
        Task<ResponseDto<CompanyOverviewDto>> GetCurrentSubscription();
        Task<ResponseDto<List<CompanyOverviewDto>>> GetCompanies();
        Task<ResponseDto<string>> SetCompanySuspension(string companyId, bool suspended);
    }
}