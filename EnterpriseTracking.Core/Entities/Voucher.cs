using EnterpriseTracking.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Entities
{
    public class  Voucher : BaseEntity
    {
        public string CompanyId { get; set; } = string.Empty;
        public Company Company { get; set; } = null!;
        public string Code { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; }
        public bool IsLinked { get; set; } = false;
        public string Status { get; set; } = VoucherStatus.Unlinked.ToString();
        public DateTime? LinkedDate { get; set; }
        public DateTime ExpiredDate { get; set; }
    }
}
