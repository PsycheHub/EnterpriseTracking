using EnterpriseTracking.Core.Dto.Request.Mailing;
using EnterpriseTracking.Core.OtherService.Interface;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class EmailService : IEmailServices
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void SendEmail(Message message)
        {
            var emailMessage = CreateEmailMessage(message);
            Send(emailMessage);
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("noreply", _configuration["EmailConfiguration:UserName"]));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };
            return emailMessage;
        }

        public void Send(MimeMessage mailMessage)
        {
            using var client = new SmtpClient();
            try
            {
                client.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                client.Connect(_configuration["EmailConfiguration:Host"], int.Parse(_configuration["EmailConfiguration:Port"]), false);
        
                client.Authenticate(_configuration["EmailConfiguration:CompanyEmailKey"], _configuration["EmailConfiguration:Password"]);
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            finally
            {
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}
