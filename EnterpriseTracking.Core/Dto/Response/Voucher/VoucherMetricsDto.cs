using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.Voucher
{
    public class VoucherMetricsDto
    {
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int ExpiringSoonVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
    }
}
