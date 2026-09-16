using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Dto.Request.Billing;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/billing")]
    public class BillingController : BaseController
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet("subscription")]
        [Authorize(Roles = "CompanyAdmin,Admin")]
        public async Task<IActionResult> GetSubscription() => HandleResponse(await _billingService.GetCurrentSubscription());

        [HttpPost("initialize")]
        [Authorize(Roles = "CompanyAdmin,Admin")]
        public async Task<IActionResult> Initialize(InitializePaymentDto request) =>
            HandleResponse(await _billingService.InitializePayment(request));

        [HttpPost("verify/{reference}")]
        [Authorize(Roles = "CompanyAdmin,Admin")]
        public async Task<IActionResult> Verify(string reference) =>
            HandleResponse(await _billingService.VerifyPayment(reference));

        [AllowAnonymous]
        [HttpPost("webhook/paystack")]
        public async Task<IActionResult> PaystackWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signature = Request.Headers["x-paystack-signature"].ToString();
            return HandleResponse(await _billingService.ProcessWebhook(payload, signature));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("admin/companies")]
        public async Task<IActionResult> GetCompanies() => HandleResponse(await _billingService.GetCompanies());

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("admin/companies/{companyId}/suspension")]
        public async Task<IActionResult> SetSuspension(string companyId, bool suspended) =>
            HandleResponse(await _billingService.SetCompanySuspension(companyId, suspended));
    }
}