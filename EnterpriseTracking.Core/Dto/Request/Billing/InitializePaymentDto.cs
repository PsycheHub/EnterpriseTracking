using System.ComponentModel.DataAnnotations;

namespace EnterpriseTracking.Core.Dto.Request.Billing
{
    public class InitializePaymentDto
    {
        [Range(1, 10000)]
        public int Seats { get; set; }

        [Required]
        public string Plan { get; set; } = string.Empty;
    }
}