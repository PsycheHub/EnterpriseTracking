using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IHelperServ
    {
        string GenerateRandomString(int length);
        string UsernameGenerator(string email);
        string FormatTime(int totalSeconds);
        string FormatLastActive(DateTime? date);
        decimal CalculateGrowthPercentage(int current, int previous);
        string FormatHourRange(int hour);
    }
}

