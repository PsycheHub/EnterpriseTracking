using System.ComponentModel.DataAnnotations;

namespace EnterpriseTracking.Core.Dto.Request.Auth
{
    public class SignUp
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
      


    }
}
