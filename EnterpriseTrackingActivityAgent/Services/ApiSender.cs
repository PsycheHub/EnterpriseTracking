using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;
using System.Text.Json;

namespace EnterpriseTrackingActivityAgent.Services
{
    public class ApiSender
    {
        private readonly RestClient _client;
        private readonly ILogger<ApiSender> _logger;
        private readonly SessionManager _sessionManager;

        private readonly string _activityEndpoint = "/api/activity/publish";
        private readonly string _screenshotEndpoint = "/api/activity/upload-screenshot";

        public ApiSender(ILogger<ApiSender> logger, SessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;

            var options = new RestClientOptions("https://enterprisetracking.onrender.com/")
            {
                // ⚠️ Only enable in development if needed
                // RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };

            _client = new RestClient(options);
        }

        private void AddAuthHeader(RestRequest request)
        {
            if (!string.IsNullOrEmpty(_sessionManager.CurrentToken))
                request.AddHeader("Authorization", $"Bearer {_sessionManager.CurrentToken}");
        }

        public async Task<bool> SendFileAsync(string filePath)
        {
            var retry = Policy
                .Handle<Exception>()
                .OrResult<bool>(r => r == false)
                .WaitAndRetryAsync(3,
                    retryAttempt => TimeSpan.FromSeconds(2),
                    (result, time, retryCount, context) =>
                    {
                        _logger.LogWarning("Retrying screenshot upload. Attempt: {RetryCount}", retryCount);
                    });

            return await retry.ExecuteAsync(async () =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File not found: {FilePath}", filePath);
                        return false;
                    }

                    var request = new RestRequest(_screenshotEndpoint, Method.Post);
                    request.AddFile("File", filePath);
                    AddAuthHeader(request);

                    _logger.LogInformation("Uploading screenshot: {FilePath}", filePath);

                    var response = await _client.ExecuteAsync(request);

                    _logger.LogInformation("Screenshot Upload Status: {StatusCode}", response.StatusCode);
                    _logger.LogDebug("Screenshot Upload Response: {Response}", response.Content);

                    if (!response.IsSuccessful)
                    {
                        _logger.LogWarning("Screenshot upload failed with status {StatusCode}", response.StatusCode);
                    }

                    return response.IsSuccessful;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading screenshot: {FilePath}", filePath);
                    return false;
                }
            });
        }

        public async Task<bool> SendAsync(ActivityEvent payload)
        {
            var retry = Policy
                .Handle<Exception>()
                .OrResult<bool>(r => r == false)
                .WaitAndRetryAsync(3,
                    retryAttempt => TimeSpan.FromSeconds(2),
                    (result, time, retryCount, context) =>
                    {
                        _logger.LogWarning("Retrying activity API call. Attempt: {RetryCount}", retryCount);
                    });

            return await retry.ExecuteAsync(async () =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(payload);

                    var request = new RestRequest(_activityEndpoint, Method.Post);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("Authorization", $"Bearer {_sessionManager.CurrentToken}");
                    request.AddStringBody(json, DataFormat.Json);

                    _logger.LogInformation("Sending activity event");

                    var response = await _client.ExecuteAsync(request);

                    _logger.LogInformation("API Status: {StatusCode}", response.StatusCode);
                    _logger.LogDebug("API Response: {Response}", response.Content);

                    if (!response.IsSuccessful)
                    {
                        _logger.LogWarning("Activity API failed with status {StatusCode}", response.StatusCode);
                    }

                    return response.IsSuccessful;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending activity event");
                    return false;
                }
            });
        }
    }
}