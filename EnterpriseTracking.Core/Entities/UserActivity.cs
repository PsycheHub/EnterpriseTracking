using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Entities
{
    public class UserActivity:BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string MapCategoryId { get; set; }
        public MapAppCatories MapCategory { get; set; }
    
        public string MachineId { get; set; }

        public string AppName { get; set; }
        public string WindowTitle { get; set; }

        public string EventType { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int DurationSeconds { get; set; }

        public bool IsIdle { get; set; }

    }
}
