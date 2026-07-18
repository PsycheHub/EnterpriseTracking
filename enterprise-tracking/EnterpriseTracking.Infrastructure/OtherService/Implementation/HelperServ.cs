using EnterpriseTracking.Core.OtherService.Interface;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace EnterpriseTracking.Core.Helper
{
  
    public  class HelperServ : IHelperServ
    {
        public  string UsernameGenerator(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                throw new ArgumentException("Invalid email");

            // 1. Extract base username
            var baseName = email.Split('@')[0];

            // 2. Normalize (letters + numbers only)
            baseName = new string(baseName
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToLower();

           

            return $"{baseName}";
        }
        public decimal CalculateGrowthPercentage(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((decimal)(current - previous) / previous) * 100, 2);
        }
        public string FormatTime(int totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
        public string FormatLastActive(DateTime? date)
        {
            if (date == null) return "N/A";

            var diff = DateTime.UtcNow - date.Value;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hrs ago";

            return date.Value.ToString("MMM d");
        }
        public  string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var data = new byte[length];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);

            var result = new StringBuilder(length);
            foreach (var b in data)
                result.Append(chars[b % chars.Length]);

            return result.ToString();
        }
        public string FormatHourRange(int hour)
        {
            var startHour = ((hour % 24) + 24) % 24;
            var endHour = (startHour + 1) % 24;

            var start = DateTime.Today.AddHours(startHour).ToString("htt").ToLower();
            var end = DateTime.Today.AddHours(endHour).ToString("htt").ToLower();

            return $"{start} - {end}";
        }
    }
}
