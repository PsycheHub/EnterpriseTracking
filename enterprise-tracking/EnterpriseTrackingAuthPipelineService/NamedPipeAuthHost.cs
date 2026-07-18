using EnterpriseTrackingAuthPipelineService.Dto;
using EnterpriseTrackingAuthPipelineService.Interface;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;




namespace EnterpriseTrackingAuthPipelineService.EnterpriseTrackingActivityAgent
{
    public class NamedPipeAuthHost
    {
        private readonly IAuthService _authService;
        private readonly ISessionContext _session;
        private CancellationTokenSource? _cts;

        private const string PIPE_NAME = "EnterpriseTracking.AuthPipe";

        public NamedPipeAuthHost(IAuthService authService, ISessionContext session)
        {
            _authService = authService;
            _session = session;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private PipeSecurity BuildPipeSecurity()
        {
            var ps = new PipeSecurity();

            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var authUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            var localSystem = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);

            ps.AddAccessRule(new PipeAccessRule(everyone, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule(authUsers, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule(localSystem, PipeAccessRights.FullControl, AccessControlType.Allow));

            return ps;
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Create NamedPipeServerStream with security
                var server = NamedPipeServerStreamAcl.Create(
                    PIPE_NAME,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    BuildPipeSecurity()
                );

                await server.WaitForConnectionAsync(token);

                // Handle client connection in background
                _ = Task.Run(() => HandleClientAsync(server, token), token);
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken token)
        {
            await using var _ = pipe;

            // Only create reader immediately
            using var reader = new StreamReader(pipe, Encoding.UTF8);

            try
            {
                // Wait for client request
                var readTask = reader.ReadLineAsync();

                if (await Task.WhenAny(readTask, Task.Delay(10000, token)) != readTask)
                {
                    await SendAsync(pipe, new PipeAuthResponse
                    {
                        Success = false,
                        Message = "Timeout waiting for request"
                    });
                    return;
                }

                var requestLine = await readTask;

                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    await SendAsync(pipe, new PipeAuthResponse
                    {
                        Success = false,
                        Message = "Empty request"
                    });
                    return;
                }

                var request = JsonSerializer.Deserialize<PipeAuthRequest>(
                    requestLine,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request == null)
                {
                    await SendAsync(pipe, new PipeAuthResponse
                    {
                        Success = false,
                        Message = "Invalid request"
                    });
                    return;
                }

                // Handle request
                PipeAuthResponse response;

                if (request.Action == "get_session")
                {
                    var session = _session.GetCurrent();
                    response = session == null
                        ? new PipeAuthResponse { Success = false, Message = "No session" }
                        : new PipeAuthResponse
                        {
                            Success = true,
                            Message = "Session found",
                            Source = session.Source,
                            Username = session.Username,
                            UserId = session.UserId,
                            Jwt = session.Jwt,
                            Roles = session.Roles
                        };
                }
                else
                {
                    var result = await _authService.ValidateAsync(new AgentLoginRequest
                    {
                        Username = request.Username,
                        Password = request.Password,
                        Voucher = request.Voucher
                    }, token);

                    response = new PipeAuthResponse
                    {
                        Success = result.Success,
                        Message = result.Message,
                        Source = result.Source,
                        Username = result.Username,
                        UserId = result.UserId,
                        Jwt = result.Jwt,
                        Roles = result.Roles
                    };
                }

                // Send response AFTER processing
                await SendAsync(pipe, response);
            }
            catch (Exception ex)
            {
                await SendAsync(pipe, new PipeAuthResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // Only create StreamWriter here, send one response
        private static async Task SendAsync(NamedPipeServerStream pipe, PipeAuthResponse response)
        {
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true)
            {
                AutoFlush = true
            };

            var json = JsonSerializer.Serialize(response);
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();

            pipe.WaitForPipeDrain();
        }
    }
}



