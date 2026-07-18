using System.Runtime.InteropServices;
namespace EnterpriseTrackingActivityAgent.Services
{
    public class IdleDetector
    {
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public int GetIdleSeconds()
        {
            LASTINPUTINFO info = new();
            info.cbSize = (uint)Marshal.SizeOf(info);

            GetLastInputInfo(ref info);

            uint idle = ((uint)Environment.TickCount - info.dwTime);

            return (int)(idle / 1000);
        }
    }
}
