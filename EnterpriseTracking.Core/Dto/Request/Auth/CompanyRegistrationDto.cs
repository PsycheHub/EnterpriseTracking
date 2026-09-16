using System.ComponentModel.DataAnnotations;

namespace EnterpriseTracking.Core.Dto.Request.Auth
{
    public class CompanyRegistrationDto
    {
        [Required, MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string CompanyEmail { get; set; } = string.Empty;

        public string? RcNumber { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Range(1, 10000)]
        public int RequestedSeats { get; set; } = 1;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Terms and Conditions.")]
        public bool AcceptTerms { get; set; }
    }
}