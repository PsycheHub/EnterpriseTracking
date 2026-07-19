using EnterpriseTrackingAuthPipelineService.Cache;
using EnterpriseTrackingAuthPipelineService.Dto;
using EnterpriseTrackingAuthPipelineService.Helper;
using EnterpriseTrackingAuthPipelineService.Interface;
using Polly;
using System.Text.Json;

namespace EnterpriseTrackingAuthPipelineService.Service
{
   
    public sealed class AuthService : IAuthService
    {
        private readonly AuthApiClient _apiClient;
        private readonly AuthCacheStore _cacheStore;
        private readonly IEncryptionService _encryptionService;
        private readonly ISessionContext _sessionContext;

        public AuthService(
            AuthApiClient apiClient,
            AuthCacheStore cacheStore,
            IEncryptionService encryptionService,
            ISessionContext sessionContext)
        {
            _apiClient = apiClient;
            _cacheStore = cacheStore;
            _encryptionService = encryptionService;
            _sessionContext = sessionContext;
        }

        public async Task<AuthResultDto> ValidateAsync(AgentLoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Username and password are required.",
                    Source = "local"
                };
            }

            try
            {
                // Online first
                var apiResponse = await _apiClient.LoginAsync(request, cancellationToken);

                if (apiResponse is not null)
                {
                    if (apiResponse.StatusCode == 200 && apiResponse.Result?.Jwt is not null)
                    {
                        var jwt = apiResponse.Result.Jwt;
                        var userId = JwtHelper.GetUserId(jwt) ?? request.Username;
                        var roles = apiResponse.Result.UserRole ?? JwtHelper.GetRoles(jwt);

                        var cachedRecord = new AuthCacheRecord
                        {
                            Username = request.Username,
                            EncryptedPassword = _encryptionService.Encrypt(request.Password),
                            EncryptedUserId = _encryptionService.Encrypt(userId),
                            EncryptedJwt = _encryptionService.Encrypt(jwt),
                            EncryptedRoles = _encryptionService.Encrypt(JsonSerializer.Serialize(roles)),
                            LastLoginUtc = DateTime.UtcNow.ToString("O")
                        };

                        await _cacheStore.UpsertAsync(cachedRecord);

                        var result = new AuthResultDto
                        {
                            Success = true,
                            Message = apiResponse.DisplayMessage ?? "Successfully login",
                            Source = "online",
                            Username = request.Username,
                            UserId = userId,
                            Jwt = jwt,
                            Roles = roles
                        };

                        _sessionContext.SetCurrent(result);
                        return result;
                    }

                    // Server replied, but login failed. Do not fallback to offline for bad credentials.
                    return new AuthResultDto
                    {
                        Success = false,
                        Message = apiResponse.ErrorMessages?.FirstOrDefault()
                                  ?? apiResponse.DisplayMessage
                                  ?? "Invalid username or password",
                        Source = "online",
                        Username = request.Username
                    };
                }
            }
            catch
            {
                // Transport error or server unreachable -> fallback to local cache
            }

            return await ValidateOfflineAsync(request);
        }

        private async Task<AuthResultDto> ValidateOfflineAsync(AgentLoginRequest request)
        {
            var cached = await _cacheStore.FindByUsernameAsync(request.Username);

            if (cached is null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Offline mode: user is not cached on this device.",
                    Source = "offline",
                    Username = request.Username
                };
            }

            string storedPassword;
            string storedUserId;
            string storedJwt;
            List<string> storedRoles = new();

            try
            {
                storedPassword = _encryptionService.Decrypt(cached.EncryptedPassword);
                storedUserId = _encryptionService.Decrypt(cached.EncryptedUserId);
                storedJwt = _encryptionService.Decrypt(cached.EncryptedJwt);

                if (!string.IsNullOrWhiteSpace(cached.EncryptedRoles))
                {
                    var rolesJson = _encryptionService.Decrypt(cached.EncryptedRoles);
                    storedRoles = JsonSerializer.Deserialize<List<string>>(rolesJson) ?? new List<string>();
                }
            }
            catch
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Offline cache is unreadable.",
                    Source = "offline",
                    Username = request.Username
                };
            }

            if (!string.Equals(storedPassword, request.Password, StringComparison.Ordinal))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Invalid username or Password",
                    Source = "offline",
                    Username = request.Username
                };
            }

            var result = new AuthResultDto
            {
                Success = true,
                Message = "Offline login successful",
                Source = "offline",
                Username = request.Username,
                UserId = storedUserId,
                Jwt = storedJwt,
                Roles = storedRoles
            };

            _sessionContext.SetCurrent(result);
            return result;
        }

        public Task<AuthResultDto?> GetCurrentSessionAsync()
        {
            return Task.FromResult(_sessionContext.GetCurrent());
        }
    }
}
