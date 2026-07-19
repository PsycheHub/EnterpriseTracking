using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class UserActivityListDto
    {
        public int Rank { get; set; }
        public string FullName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string TotalActivityTime { get; set; }
        public string AvgDailyTime { get; set; }
        public string AvgIdleTime { get; set; }
        public int ActivityProofCount { get; set; }
        public string LastActive { get; set; }
    }
}
