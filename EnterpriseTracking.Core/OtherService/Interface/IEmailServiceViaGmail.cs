using EnterpriseTracking.Core.Dto.Request.Mailing;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IEmailServiceViaGmail
    {
        void SendEmail(Message message);
    }
}
