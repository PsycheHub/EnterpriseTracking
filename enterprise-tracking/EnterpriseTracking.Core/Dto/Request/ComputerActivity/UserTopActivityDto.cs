using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Request.ComputerActivity
{
    public class UserTopActivityDto
    {
        public int No { get; set; }
        public string AppName { get; set; }
        public string Category { get; set; }
        public int ActivityProofCount { get; set; }
        public string TimeSpent { get; set; }
        public double PercentageUsage { get; set; }
    }
}
