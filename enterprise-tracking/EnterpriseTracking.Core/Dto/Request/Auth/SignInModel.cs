using System.ComponentModel.DataAnnotations;

namespace EnterpriseTracking.Core.Dto.Request.Auth
{
    public class SignInModel
    {
        [Required]
       
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
