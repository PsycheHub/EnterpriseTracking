using EnterpriseTrackingActivityAgent.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;

namespace EnterpriseTrackingActivityAgent
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly ILogger<TrayApplicationContext> _logger;
        private readonly AgentService _agentService;
        private NotifyIcon? _trayIcon;
        private readonly ToolStripMenuItem _statusMenuItem;
        private ToolStripMenuItem? _autoStartMenuItem;

        public TrayApplicationContext(ILogger<TrayApplicationContext> logger, AgentService agentService)
        {
            _logger = logger;
            _agentService = agentService;

            _statusMenuItem = new ToolStripMenuItem("Status: Running");

            InitializeTrayIcon();
            StartAgent();
        }

        private void InitializeTrayIcon()
        {
            // Use system application icon as fallback; replace with your own .ico file if available
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Enterprise Activity Agent",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            // Status indicator (non-clickable)
            _trayIcon.ContextMenuStrip.Items.Add(_statusMenuItem);
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            // Auto-start toggle
            _autoStartMenuItem = new ToolStripMenuItem("Start with Windows")
            {
                Checked = IsAutoStartEnabled(),
                CheckOnClick = true
            };
            _autoStartMenuItem.Click += (s, e) => ToggleAutoStart();
            _trayIcon.ContextMenuStrip.Items.Add(_autoStartMenuItem);

            _trayIcon.ContextMenuStrip.Items.Add("Open Log Folder", null, (s, e) => OpenLogFolder());
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => ExitApplication());

            _trayIcon.DoubleClick += (s, e) => OpenLogFolder();

            _logger.LogInformation("Tray icon initialized.");
        }

        private void StartAgent()
        {
            try
            {
                _agentService.Start();
                _statusMenuItem.Text = "Status: Running";
                _logger.LogInformation("Agent started successfully.");
            }
            catch (Exception ex)
            {
                _statusMenuItem.Text = "Status: Error";
                _logger.LogError(ex, "Failed to start agent.");
                MessageBox.Show($"Failed to start agent: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenLogFolder()
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            Process.Start("explorer.exe", logDir);
        }

        private bool IsAutoStartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            return key?.GetValue("EnterpriseActivityAgent") != null;
        }

        private void ToggleAutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (_autoStartMenuItem!.Checked)
            {
                var exePath = Application.ExecutablePath;
                key.SetValue("EnterpriseActivityAgent", $"\"{exePath}\"");
                _logger.LogInformation("Auto-start enabled.");
            }
            else
            {
                key.DeleteValue("EnterpriseActivityAgent", false);
                _logger.LogInformation("Auto-start disabled.");
            }
        }

        private void ExitApplication()
        {
            _logger.LogInformation("Exiting application...");
            _trayIcon!.Visible = false;
            _agentService.Stop();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _agentService?.Stop();
            }
            base.Dispose(disposing);
        }
    }
}