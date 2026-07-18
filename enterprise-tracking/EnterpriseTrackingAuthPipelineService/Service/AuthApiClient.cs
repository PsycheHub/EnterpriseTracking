using EnterpriseTrackingAuthPipelineService.Dto;
using RestSharp;
using System.Text.Json;

namespace EnterpriseTrackingAuthPipelineService.Service
{




    public sealed class AuthApiClient
    {
        private readonly RestClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthApiClient()
        {
            var baseUrl = "https://enterprise-tracking-0ibo.onrender.com";

            var options = new RestClientOptions(baseUrl)
            {
                // Dev only if your local cert is self-signed
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            };

            _client = new RestClient(options);
        }

        public async Task<AgentLoginApiResponse?> LoginAsync(AgentLoginRequest request, CancellationToken cancellationToken = default)
        {
            var restRequest = new RestRequest("/api/account/agent/user/login", Method.Post);
            restRequest.AddHeader("accept", "*/*");
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddJsonBody(request);

            var response = await _client.ExecuteAsync(restRequest, cancellationToken);

            if (string.IsNullOrWhiteSpace(response.Content))
                return null;

            return JsonSerializer.Deserialize<AgentLoginApiResponse>(response.Content, JsonOptions);
        }
    }
}
