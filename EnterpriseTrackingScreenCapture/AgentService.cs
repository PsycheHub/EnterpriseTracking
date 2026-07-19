using EnterpriseTrackingScreenCapture.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EnterpriseTrackingScreenCapture
{
    public class AgentService
    {
        private readonly ILogger<AgentService> _logger;
        private System.Timers.Timer? timer;
        private bool isRunning = false;
        private readonly ActiveWindowService window = new();
        private readonly ApiSender sender;
        private readonly ScreenshotService shot = new();
        private readonly Random random = new();
        private string? currentApp;
        private string? currentTitle;
        private DateTime start;
        private DateTime nextScreenshotTime = DateTime.UtcNow;
        private readonly string screenshotDir;
        private readonly HttpClient httpClient = new();

        // Offline backoff state
        private int offlineRetryCount = 0;
        private const int maxBackoffMs = 30 * 60 * 1000; // 30 minutes
        private const int initialBackoffMs = 5000;       // 5 seconds

        public AgentService(ILogger<AgentService> logger, ApiSender sender)
        {
            _logger = logger;
            screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screens");
            httpClient.Timeout = TimeSpan.FromSeconds(3);
            this.sender = sender;
        }

        private string? GetCurrentUserId()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EnterpriseTrackingAgent",
                "current_user_id.txt");

            try
            {
                if (File.Exists(path))
                {
                    string userId = File.ReadAllText(path, Encoding.UTF8).Trim();
                    _logger.LogInformation("Reading UserId file from {Path}", path);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _logger.LogInformation("UserId read (UTF-8): {UserId}", userId);
                        return userId;
                    }

                    byte[] raw = File.ReadAllBytes(path);
                    if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
                    {
                        userId = Encoding.Unicode.GetString(raw).Trim();
                        _logger.LogInformation("UserId read (UTF-16LE with BOM): {UserId}", userId);
                    }
                    else
                    {
                        userId = Encoding.Unicode.GetString(raw).Trim();
                        _logger.LogInformation("UserId read (UTF-16LE no BOM): {UserId}", userId);
                    }
                    return string.IsNullOrEmpty(userId) ? null : userId;
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading UserId from {Path}", path);
            }
            return null;
        }

        public void Start()
        {
            if (timer != null)
            {
                _logger.LogWarning("Agent is already running.");
                return;
            }

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += async (s, e) =>
            {
                if (!isRunning)
                    await Run();
            };
            timer.Start();
            ScheduleNextScreenshot();
            _ = Task.Run(ProcessOfflineScreenshots);
            _logger.LogInformation("Agent started successfully.");
        }

        public void Stop()
        {
            if (timer == null)
            {
                _logger.LogWarning("Agent is not running.");
                return;
            }

            timer.Stop();
            timer.Dispose();
            timer = null;
            _logger.LogInformation("Agent stopped.");
        }

        private async Task Run()
        {
            if (isRunning) return;
            try
            {
                isRunning = true;
                _logger.LogDebug("Run cycle started.");

                var active = window.GetActiveWindow();
                string? userId = GetCurrentUserId();

                if (currentApp == null)
                {
                    currentApp = active.app;
                    currentTitle = active.title;
                    start = DateTime.UtcNow;
                    _logger.LogDebug("Initial active window recorded: App={App}, Title={Title}", currentApp, currentTitle);
                    return;
                }

                // Screenshot capture
                if (DateTime.UtcNow >= nextScreenshotTime)
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _logger.LogInformation("Triggering screenshot capture for app={App}, userId={UserId}", active.app, userId);
                        _ = Task.Run(() => HandleScreenshot(active.app, userId));
                    }
                    else
                    {
                        _logger.LogWarning("No valid UserId – skipping screenshot.");
                    }
                    ScheduleNextScreenshot();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Run cycle encountered an error.");
            }
            finally
            {
                isRunning = false;
                _logger.LogDebug("Run cycle finished.");
            }
        }

        // ===========================
        // SCREENSHOT CAPTURE (SAVE ONLY)
        // ===========================
        private async Task HandleScreenshot(string appName, string userId)
        {
            try
            {
                string path = shot.Capture(appName, userId);
                _logger.LogInformation("Screenshot captured and saved to disk: {Path}", path);
                // No immediate upload – offline processor will handle it when online.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture screenshot for app={App}, userId={UserId}", appName, userId);
            }
        }

        // ===========================
        // UNIFIED UPLOAD LOGIC
        // ===========================
        private async Task<bool> UploadScreenshot(string path)
        {
            try
            {
                _logger.LogInformation("Attempting to upload screenshot: {Path}", path);
                bool sent = await sender.SendFileAsync(path);
                if (sent && File.Exists(path))
                {
                    File.Delete(path);
                    _logger.LogInformation("Screenshot uploaded and deleted successfully: {Path}", path);
                    return true;
                }
                _logger.LogWarning("Upload failed for {Path} (server returned false or file missing)", path);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during upload of {Path}", path);
                return false;
            }
        }

        private void ScheduleNextScreenshot()
        {
            int seconds = random.Next(130, 500);
            nextScreenshotTime = DateTime.UtcNow.AddSeconds(seconds);
            _logger.LogDebug("Next screenshot scheduled in {Seconds} seconds (at {Time})", seconds, nextScreenshotTime);
        }

        // ===========================
        // CONNECTIVITY CHECK
        // ===========================
        private async Task<bool> IsServerReachableAsync()
        {
            try
            {
                _logger.LogDebug("Checking server connectivity to https://enterprise-tracking.onrender.com/api/ping");
                var request = new HttpRequestMessage(HttpMethod.Get, "https://enterprise-tracking.onrender.com/api/ping");
                var response = await httpClient.SendAsync(request);
                bool reachable = response.IsSuccessStatusCode;
                _logger.LogDebug("Connectivity check result: {Reachable}", reachable);
                return reachable;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Connectivity check failed: {Message}", ex.Message);
                return false;
            }
        }

        // ===========================
        // OFFLINE PROCESSOR WITH INCREMENTAL BACKOFF
        // ===========================
        private async Task ProcessOfflineScreenshots()
        {
            _logger.LogInformation("Offline screenshot processor started.");

            while (true)
            {
                try
                {
                    if (!Directory.Exists(screenshotDir))
                    {
                        _logger.LogDebug("Screenshot directory does not exist yet: {Dir}", screenshotDir);
                        await Task.Delay(15000);
                        continue;
                    }

                    bool online = await IsServerReachableAsync();

                    if (!online)
                    {
                        // Incremental backoff when offline
                        int delayMs = Math.Min(initialBackoffMs * (int)Math.Pow(2, offlineRetryCount), maxBackoffMs);
                        offlineRetryCount++;
                        _logger.LogWarning("No internet connection (offline attempt #{Count}). Waiting {DelayMs} ms before next check.",
                            offlineRetryCount, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    // Online: reset backoff counter and process files
                    if (offlineRetryCount > 0)
                    {
                        _logger.LogInformation("Connectivity restored after {FailedAttempts} failed attempts. Resetting backoff.", offlineRetryCount);
                        offlineRetryCount = 0;
                    }

                    var files = Directory.GetFiles(screenshotDir, "*.jpg");
                    _logger.LogInformation("Found {FileCount} pending screenshot(s) to upload.", files.Length);

                    if (files.Length == 0)
                    {
                        await Task.Delay(5000);
                        continue;
                    }

                    foreach (var file in files)
                    {
                        try
                        {
                            if (IsFileLocked(file))
                            {
                                _logger.LogDebug("File is locked, skipping: {File}", file);
                                continue;
                            }

                            bool uploaded = await UploadScreenshot(file);
                            if (!uploaded)
                            {
                                _logger.LogWarning("Failed to upload {File}, will retry later.", file);
                                // If upload fails (e.g., server error), keep the file and try again after short delay
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing screenshot file {File}", file);
                        }
                    }

                    // Short delay before scanning again for new files
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Offline screenshot processor encountered a critical error.");
                    await Task.Delay(15000);
                }
            }
        }

        // ===========================
        // FILE LOCK CHECK
        // ===========================
        private bool IsFileLocked(string file)
        {
            try
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}