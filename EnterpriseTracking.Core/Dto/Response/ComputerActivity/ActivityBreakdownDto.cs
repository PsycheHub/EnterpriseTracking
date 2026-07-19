using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ActivityBreakdownDto
    {
        public List<ChartItemDto> TopApps { get; set; }
        public List<ChartItemDto> TopCategories { get; set; }
    }

    public class ChartItemDto
    {
        public string Name { get; set; }
        public int TotalSeconds { get; set; }
        public string FormattedTime { get; set; }
    }
}
