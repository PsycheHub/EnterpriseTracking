using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.Attendance
{
    public class AttendanceRecordDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public DateTime FirstLogin { get; set; }
        public DateTime? LastActivity { get; set; }
        public string TotalActivityTime { get; set; }
        public string IdleTime { get; set; }
    }
}
