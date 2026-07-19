using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.Voucher
{
    public class VoucherRespDto
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string LinkedTo { get; set; }
        public string Status { get; set; }
        public string ValidatyPercentage { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expired { get; set; }
    }
}
