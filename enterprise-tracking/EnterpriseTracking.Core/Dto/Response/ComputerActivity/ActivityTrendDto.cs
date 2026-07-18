using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ActivityTrendDto
    {
        public string Label { get; set; }          // "Jan 1" OR "Jan"
        public int TotalSeconds { get; set; }
        public string FormattedTime { get; set; }
    }
}
