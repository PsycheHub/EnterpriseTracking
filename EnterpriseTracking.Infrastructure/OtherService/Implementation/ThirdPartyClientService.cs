using EnterpriseTracking.Core.Dto.Request.Integration;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Integration;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class ThirdPartyClientService : IThirdPartyClientService
    {
        private readonly EnterpriseTrackingContext _context;
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;

        public ThirdPartyClientService(EnterpriseTrackingContext context, IConfiguration configuration, ITenantContext tenantContext)
        {
            _context = context;
            _configuration = configuration;
            _tenantContext = tenantContext;
        }

        public async Task<ResponseDto<CreatedClientDto>> CreateClientAsync(CreateThirdPartyClientReq req, string adminId)
        {
            var clientId = $"ogvx_{Guid.NewGuid():N}";
            var rawSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(40));
            var secretHash = HashSecret(rawSecret);

            var entity = new ThirdPartyClient
            {
                Id = Guid.NewGuid().ToString(),
                CompanyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("A company account is required"),
                AppName = req.AppName,
                ContactEmail = req.ContactEmail,
                ClientId = clientId,
                ClientSecretHash = secretHash,
                Permissions = req.Permissions,
                IsActive = true,
                CreatedByAdminId = adminId
            };

            await _context.ThirdPartyClients.AddAsync(entity);
            await _context.SaveChangesAsync();

            return new ResponseDto<CreatedClientDto>
            {
                StatusCode = 200,
                DisplayMessage = "Client created. Store the client secret securely — it will not be shown again.",
                Result = new CreatedClientDto
                {
                    Id = entity.Id,
                    AppName = entity.AppName,
                    ContactEmail = entity.ContactEmail,
                    ClientId = clientId,
                    ClientSecret = rawSecret,
                    Permissions = entity.Permissions,
                    Created = entity.Created
                }
            };
        }

        public async Task<ResponseDto<List<ThirdPartyClientDto>>> ListClientsAsync()
        {
            var clients = await _context.ThirdPartyClients
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.Created)
                .Select(c => new ThirdPartyClientDto
                {
                    Id = c.Id,
                    AppName = c.AppName,
                    ContactEmail = c.ContactEmail,
                    ClientId = c.ClientId,
                    IsActive = c.IsActive,
                    Permissions = c.Permissions,
                    LastUsed = c.LastUsed,
                    Created = c.Created
                })
                .ToListAsync();

            return new ResponseDto<List<ThirdPartyClientDto>>
            {
                StatusCode = 200,
                DisplayMessage = "Success",
                Result = clients
            };
        }

        public async Task<ResponseDto<string>> RevokeClientAsync(string id)
        {
            var client = await _context.ThirdPartyClients.FirstOrDefaultAsync(x => x.Id == id);
            if (client == null || client.IsDeleted)
                return new ResponseDto<string> { StatusCode = 404, DisplayMessage = "Client not found." };

            client.IsActive = false;
            client.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ResponseDto<string> { StatusCode = 200, DisplayMessage = "Client revoked.", Result = id };
        }

        public async Task<ResponseDto<CreatedClientDto>> RegenerateSecretAsync(string id)
        {
            var client = await _context.ThirdPartyClients.FirstOrDefaultAsync(x => x.Id == id);
            if (client == null || client.IsDeleted)
                return new ResponseDto<CreatedClientDto> { StatusCode = 404, DisplayMessage = "Client not found." };

            var rawSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(40));
            client.ClientSecretHash = HashSecret(rawSecret);
            client.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ResponseDto<CreatedClientDto>
            {
                StatusCode = 200,
                DisplayMessage = "Secret regenerated. Store it securely — it will not be shown again.",
                Result = new CreatedClientDto
                {
                    Id = client.Id,
                    AppName = client.AppName,
                    ContactEmail = client.ContactEmail,
                    ClientId = client.ClientId,
                    ClientSecret = rawSecret,
                    Permissions = client.Permissions,
                    Created = client.Created
                }
            };
        }

        public async Task<ResponseDto<ClientTokenDto>> ExchangeTokenAsync(ClientTokenRequest req)
        {
            var client = await _context.ThirdPartyClients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.ClientId == req.ClientId && !c.IsDeleted && c.IsActive);

            if (client == null)
                return new ResponseDto<ClientTokenDto> { StatusCode = 401, DisplayMessage = "Invalid credentials." };

            if (!VerifySecret(req.ClientSecret, client.ClientSecretHash))
                return new ResponseDto<ClientTokenDto> { StatusCode = 401, DisplayMessage = "Invalid credentials." };

            client.LastUsed = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            const int expiresInSeconds = 3600;
            var token = BuildToken(client, expiresInSeconds);

            return new ResponseDto<ClientTokenDto>
            {
                StatusCode = 200,
                DisplayMessage = "Token issued.",
                Result = new ClientTokenDto
                {
                    AccessToken = token,
                    ExpiresInSeconds = expiresInSeconds
                }
            };
        }

        private string BuildToken(ThirdPartyClient client, int expiresInSeconds)
        {
            var scopes = client.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, client.ClientId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("client_name", client.AppName),
                new("company_id", client.CompanyId),
                new(ClaimTypes.Role, "ThirdParty")
            };
            foreach (var scope in scopes)
                claims.Add(new Claim("scope", scope.Trim()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
                claims: claims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha384Signature));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashSecret(string secret)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(bytes);
        }

        private static bool VerifySecret(string provided, string storedHash)
        {
            var providedHash = HashSecret(provided);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash),
                Encoding.UTF8.GetBytes(storedHash));
        }
    }
}
