using EnterpriseTracking.Core.Dto.Request.Mailing;
using EnterpriseTracking.Core.OtherService.Interface;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class EmailServiceViaGmail : IEmailServiceViaGmail, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailServiceViaGmail> _logger;
        private readonly GmailService _gmailService;
        private readonly string _visibleFromEmail;   // ← what users actually see

        public EmailServiceViaGmail(IConfiguration configuration, ILogger<EmailServiceViaGmail> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load Gmail API credentials
            var clientId = _configuration["EmailConfiguration:ClientId"]
                ?? throw new ArgumentNullException("EmailConfiguration:ClientId");
            var clientSecret = _configuration["EmailConfiguration:ClientSecret"]
                ?? throw new ArgumentNullException("EmailConfiguration:ClientSecret");
            var refreshToken = _configuration["EmailConfiguration:RefreshToken"]
                ?? throw new ArgumentNullException("EmailConfiguration:RefreshToken");

            _visibleFromEmail = _configuration["EmailConfiguration:FromEmail"] ?? "noreply@zed.ng";

            // === REAL USER FOR IMPERSONATION (never shown to users) ===
            const string impersonatedUser = "product@zed.ng";

            // Build Gmail service with refresh token
            var clientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { GmailService.Scope.GmailSend }
            });

            var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
            var credential = new UserCredential(flow, impersonatedUser, tokenResponse);

            _gmailService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Zed.ng Email Service"
            });
        }

        public void SendEmail(Message message)
        {
            var emailMessage = CreateEmailMessage(message);
            Send(emailMessage);
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("noreply", _visibleFromEmail));  // ← now shows noreply@zed.ng
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };
            return emailMessage;
        }

        public void Send(MimeMessage mailMessage)
        {
            try
            {
                // Convert MimeMessage → Gmail API raw format
                var rawMessage = ConvertToGmailRaw(mailMessage);

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };
                _gmailService.Users.Messages.Send(gmailMessage, "me").Execute();

                _logger.LogInformation("Email sent successfully via Gmail API to {To} from {From}",
                    string.Join(", ", mailMessage.To), _visibleFromEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via Gmail API");
            }
        }

        private static string ConvertToGmailRaw(MimeMessage mimeMessage)
        {
            using var stream = new MemoryStream();
            mimeMessage.WriteTo(stream);
            var base64 = Convert.ToBase64String(stream.ToArray());

            // Gmail requires base64url encoding
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public void Dispose()
        {
            _gmailService?.Dispose();
        }
    }
}
