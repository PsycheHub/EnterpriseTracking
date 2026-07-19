using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Request.ComputerActivity
{
    public class CreateMapAppCategoryReqDto
    {
        public string Name { get; set; }
        public string? IconUrl { get; set; }
        public string EventType { get; set; }
        public string CategoryId { get; set; }
    }
}
