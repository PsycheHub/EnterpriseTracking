using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EnterpriseTrackingScreenCapture.Services
{




    public class ActiveWindowService
    {
        [DllImport("user32.dll")]
        static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

        public (string app, string title) GetActiveWindow()
        {
            nint handle = GetForegroundWindow();

            StringBuilder title = new(1024);
            GetWindowText(handle, title, title.Capacity);

            GetWindowThreadProcessId(handle, out uint pid);

            try
            {
                var process = Process.GetProcessById((int)pid);
                return (process.ProcessName, title.ToString());
            }
            catch
            {
                return ("unknown", title.ToString());
            }
        }
    }
}
