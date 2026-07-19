using System.Drawing;
using System.Drawing.Imaging;

namespace EnterpriseTrackingScreenCapture.Services
{
    public class ScreenshotService
    {
        private readonly string _folder;
        private readonly Random _random = new();

        public ScreenshotService()
        {
            _folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screens");

            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);
        }

        public string Capture(string appName, string userId)
        {
            Rectangle bounds = SystemInformation.VirtualScreen;

            using Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

            // ✅ sanitize app name (important)
            appName = Sanitize(appName);

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            int rand = _random.Next(1000000, 999999999);

            string fileName = $"{appName}@{timestamp}@{userId}@{rand}.jpg";

            string path = Path.Combine(_folder, fileName);

            bitmap.Save(path, ImageFormat.Jpeg);

            return path;
        }

        private string Sanitize(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');

            return input.Replace(" ", "_");
        }
    }
}