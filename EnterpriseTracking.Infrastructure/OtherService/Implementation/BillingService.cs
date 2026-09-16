using EnterpriseTracking.Core.Dto.Request.Billing;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Billing;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class BillingService : IBillingService
    {
        private readonly EnterpriseTrackingContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BillingService(
            EnterpriseTrackingContext context,
            ITenantContext tenantContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _tenantContext = tenantContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<ResponseDto<PaymentInitializationDto>> InitializePayment(InitializePaymentDto request)
        {
            var response = new ResponseDto<PaymentInitializationDto>();
            var company = await _context.Set<Company>().FirstOrDefaultAsync(x => x.Id == _tenantContext.CompanyId);
            if (company == null || company.IsSuspended)
                return Error<PaymentInitializationDto>(StatusCodes.Status403Forbidden, "Company account is unavailable");

            var pricePerSeatKobo = _configuration.GetValue<long>("Paystack:PricePerSeatKobo", 500000);
            var reference = $"OGV-{company.Id}-{Guid.NewGuid():N}";
            var amount = checked(pricePerSeatKobo * request.Seats);
            var callbackUrl = _configuration["Paystack:CallbackUrl"] ?? "http://localhost:3000/dashboard/billing/callback";

            var client = CreatePaystackClient();
            var paystackResponse = await client.PostAsJsonAsync("transaction/initialize", new
            {
                email = company.Email,
                amount,
                reference,
                callback_url = callbackUrl,
                metadata = new { companyId = company.Id, request.Seats, request.Plan }
            });
            if (!paystackResponse.IsSuccessStatusCode)
                return Error<PaymentInitializationDto>(StatusCodes.Status502BadGateway, "Unable to initialize Paystack payment");

            using var payload = JsonDocument.Parse(await paystackResponse.Content.ReadAsStringAsync());
            var data = payload.RootElement.GetProperty("data");
            _context.Set<PaymentTransaction>().Add(new PaymentTransaction
            {
                CompanyId = company.Id,
                Reference = reference,
                AmountKobo = amount,
                Seats = request.Seats,
                Plan = request.Plan.Trim()
            });
            await _context.SaveChangesAsync();

            response.StatusCode = StatusCodes.Status200OK;
            response.DisplayMessage = "Payment initialized";
            response.Result = new PaymentInitializationDto
            {
                AuthorizationUrl = data.GetProperty("authorization_url").GetString() ?? string.Empty,
                AccessCode = data.GetProperty("access_code").GetString() ?? string.Empty,
                Reference = reference
            };
            return response;
        }

        public async Task<ResponseDto<string>> VerifyPayment(string reference)
        {
            var transaction = await _context.Set<PaymentTransaction>()
                .FirstOrDefaultAsync(x => x.Reference == reference && x.CompanyId == _tenantContext.CompanyId);
            if (transaction == null)
                return Error<string>(StatusCodes.Status404NotFound, "Payment transaction was not found");
            if (transaction.Status == "Success")
                return Success("Payment was already verified");

            var paystackResponse = await CreatePaystackClient().GetAsync($"transaction/verify/{Uri.EscapeDataString(reference)}");
            if (!paystackResponse.IsSuccessStatusCode)
                return Error<string>(StatusCodes.Status502BadGateway, "Unable to verify Paystack payment");

            using var payload = JsonDocument.Parse(await paystackResponse.Content.ReadAsStringAsync());
            var data = payload.RootElement.GetProperty("data");
            var status = data.GetProperty("status").GetString();
            var amount = data.GetProperty("amount").GetInt64();
            if (status != "success" || amount != transaction.AmountKobo)
                return Error<string>(StatusCodes.Status400BadRequest, "Payment has not been completed");

            await ApplySuccessfulPayment(transaction);
            return Success($"Payment verified. {transaction.Seats} seats added.");
        }

        public async Task<ResponseDto<string>> ProcessWebhook(string payload, string signature)
        {
            var expectedSignature = Convert.ToHexString(
                HMACSHA512.HashData(Encoding.UTF8.GetBytes(GetPaystackSecret()), Encoding.UTF8.GetBytes(payload)))
                .ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedSignature),
                Encoding.ASCII.GetBytes(signature.ToLowerInvariant())))
                return Error<string>(StatusCodes.Status401Unauthorized, "Invalid Paystack signature");

            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.GetProperty("event").GetString() != "charge.success")
                return Success("Event ignored");

            var data = document.RootElement.GetProperty("data");
            var reference = data.GetProperty("reference").GetString();
            var amount = data.GetProperty("amount").GetInt64();
            var transaction = await _context.Set<PaymentTransaction>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Reference == reference);
            if (transaction == null || transaction.AmountKobo != amount)
                return Error<string>(StatusCodes.Status400BadRequest, "Payment transaction mismatch");
            if (transaction.Status != "Success")
                await ApplySuccessfulPayment(transaction);
            return Success("Webhook processed");
        }

        public async Task<ResponseDto<CompanyOverviewDto>> GetCurrentSubscription()
        {
            var company = await CompanyQuery().FirstOrDefaultAsync(x => x.Id == _tenantContext.CompanyId);
            if (company == null)
                return Error<CompanyOverviewDto>(StatusCodes.Status404NotFound, "Company was not found");
            return Ok(await MapCompany(company));
        }

        public async Task<ResponseDto<List<CompanyOverviewDto>>> GetCompanies()
        {
            if (!_tenantContext.IsSuperAdmin)
                return Error<List<CompanyOverviewDto>>(StatusCodes.Status403Forbidden, "Super administrator access is required");

            var companies = await CompanyQuery().Where(x => x.Email != "superadmin@ogavix.com").ToListAsync();
            var result = new List<CompanyOverviewDto>();
            foreach (var company in companies)
                result.Add(await MapCompany(company));
            return Ok(result);
        }

        public async Task<ResponseDto<string>> SetCompanySuspension(string companyId, bool suspended)
        {
            if (!_tenantContext.IsSuperAdmin)
                return Error<string>(StatusCodes.Status403Forbidden, "Super administrator access is required");
            var company = await _context.Set<Company>().FirstOrDefaultAsync(x => x.Id == companyId);
            if (company == null)
                return Error<string>(StatusCodes.Status404NotFound, "Company was not found");
            company.IsSuspended = suspended;
            company.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Success(suspended ? "Company suspended" : "Company restored");
        }

        private IQueryable<Company> CompanyQuery() => _context.Set<Company>()
            .Include(x => x.Subscription)
            .Include(x => x.Users)
            .Include(x => x.Vouchers);

        private async Task<CompanyOverviewDto> MapCompany(Company company)
        {
            var adminRoleIds = await _context.Roles
                .Where(x => x.Name == "CompanyAdmin" || x.Name == "Admin" || x.Name == "SuperAdmin")
                .Select(x => x.Id)
                .ToListAsync();
            var adminUserIds = await _context.UserRoles
                .Where(x => adminRoleIds.Contains(x.RoleId))
                .Select(x => x.UserId)
                .ToListAsync();
            var totalPaid = await _context.Set<PaymentTransaction>()
                .Where(x => x.CompanyId == company.Id && x.Status == "Success")
                .SumAsync(x => (long?)x.AmountKobo) ?? 0;
            return new CompanyOverviewDto
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email,
                IsSuspended = company.IsSuspended,
                Plan = company.Subscription?.Plan ?? "None",
                SeatLimit = company.Subscription?.SeatLimit ?? 0,
                UsedSeats = company.Users.Count(x => !x.IsDeleted && !x.IsSuspend && !adminUserIds.Contains(x.Id)),
                IsSubscriptionActive = company.Subscription?.IsActive == true,
                CurrentPeriodEndsAt = company.Subscription?.CurrentPeriodEndsAt ?? company.Subscription?.TrialEndsAt,
                TotalPaidKobo = totalPaid
            };
        }

        private HttpClient CreatePaystackClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.paystack.co/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetPaystackSecret());
            return client;
        }

        private string GetPaystackSecret()
        {
            var secret = _configuration["Paystack:SecretKey"] ?? Environment.GetEnvironmentVariable("PAYSTACK_SECRET_KEY");
            return string.IsNullOrWhiteSpace(secret)
                ? throw new InvalidOperationException("Paystack secret key is not configured")
                : secret;
        }

        private async Task ApplySuccessfulPayment(PaymentTransaction transaction)
        {
            var subscription = await _context.Set<CompanySubscription>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.CompanyId == transaction.CompanyId)
                ?? throw new InvalidOperationException("Company subscription was not found");
            transaction.Status = "Success";
            transaction.PaidAt = DateTime.UtcNow;
            subscription.SeatLimit += transaction.Seats;
            subscription.Plan = transaction.Plan;
            subscription.IsActive = true;
            subscription.CurrentPeriodEndsAt = DateTime.UtcNow.AddMonths(1);
            subscription.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static ResponseDto<T> Error<T>(int statusCode, string message) => new()
        {
            StatusCode = statusCode,
            DisplayMessage = message,
            ErrorMessages = new List<string> { message }
        };

        private static ResponseDto<T> Ok<T>(T result) => new()
        {
            StatusCode = StatusCodes.Status200OK,
            DisplayMessage = "Success",
            Result = result
        };

        private static ResponseDto<string> Success(string message) => Ok(message);
    }
}