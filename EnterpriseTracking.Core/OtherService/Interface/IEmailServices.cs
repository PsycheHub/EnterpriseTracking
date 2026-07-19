using EnterpriseTracking.Core.Dto.Request.Mailing;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IEmailServices
    {
        void SendEmail(Message message);
    }
}
