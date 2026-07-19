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
        private readonly SessionManager _sessionManager;
        private NotifyIcon? _trayIcon;
        private readonly ToolStripMenuItem _statusMenuItem;
        private ToolStripMenuItem? _autoStartMenuItem;

        public TrayApplicationContext(
            ILogger<TrayApplicationContext> logger,
            AgentService agentService,
            SessionManager sessionManager)
        {
            _logger = logger;
            _agentService = agentService;
            _sessionManager = sessionManager;

            _statusMenuItem = new ToolStripMenuItem("Status: Authenticating…");

            InitializeTrayIcon();

            // Show login form immediately on the STA thread before the message pump starts.
            // ShowDialog() runs its own modal message pump, so this works fine here.
            if (!ShowLoginAndAuthenticate())
            {
                _trayIcon!.Visible = false;
                Environment.Exit(0);
                return;
            }

            StartAgent();
        }

        /// <summary>
        /// Shows the login form, retrying up to 3 times on bad credentials.
        /// Returns true when authenticated, false when the user cancels.
        /// </summary>
        private bool ShowLoginAndAuthenticate()
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using var form = new LoginForm();
                if (attempt > 1)
                    form.ShowError("Invalid credentials or server error. Please try again.");

                if (form.ShowDialog() != DialogResult.OK)
                {
                    _logger.LogWarning("Login cancelled by user.");
                    return false;
                }

                bool ok = _sessionManager
                    .EnsureSessionAsync(form.Username, form.Password, form.Voucher)
                    .GetAwaiter().GetResult();

                if (ok) return true;

                _logger.LogWarning("Authentication attempt {Attempt} failed.", attempt);
            }

            MessageBox.Show(
                "Could not authenticate after 3 attempts. The agent will now exit.",
                "Authentication Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return false;
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