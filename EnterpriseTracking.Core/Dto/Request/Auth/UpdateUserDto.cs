using System.ComponentModel.DataAnnotations;

namespace EnterpriseTracking.Core.Dto.Request.Auth
{
    public class UpdateUserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }
    }
}