using EnterpriseTracking.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Request.Auth
{
    public class ExportUserRequest
    {
      
        public UserStatus? Status { get; set; }
        public bool? IsVoucherLinked { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
