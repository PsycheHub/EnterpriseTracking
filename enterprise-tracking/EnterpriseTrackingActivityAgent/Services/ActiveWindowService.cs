using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EnterpriseTrackingActivityAgent.Services
{




    public class ActiveWindowService
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public (string app, string title) GetActiveWindow()
        {
            IntPtr handle = GetForegroundWindow();

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
