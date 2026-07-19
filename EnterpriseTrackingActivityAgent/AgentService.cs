using EnterpriseTrackingActivityAgent.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using System.Text;

namespace EnterpriseTrackingActivityAgent
{
    public class AgentService
    {
        private readonly ILogger<AgentService> _logger;

        private System.Timers.Timer? timer;
        private bool isRunning = false;
        public bool IsRunning { get; private set; }

        private readonly ActiveWindowService window = new();
        private readonly IdleDetector idle = new();
        private readonly MeetingDetector meeting = new();
        private readonly ApiSender sender;
        private readonly OfflineQueue queue = new();
        private readonly LearningDetector learning = new();

        private string? currentApp;
        private string? currentTitle;
        private DateTime start;
        private string? cachedDeviceName;
        private DateTime cachedDeviceNameExpiresAt = DateTime.MinValue;
        private static readonly TimeSpan DeviceNameCacheDuration = TimeSpan.FromDays(1);
        public AgentService(ILogger<AgentService> logger, ApiSender sender)
        {
            _logger = logger;
            this.sender = sender;
        }

        private string? GetCurrentUserId()
        {
            
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EnterpriseTrackingAgent",
                "current_user_id.txt");

            _logger.LogInformation("Attempting to read user ID from: {Path}", path);

            // Retry up to 5 times (useful if the file is created after login)

            for (int attempt = 1; attempt <= 5; attempt++)
            {
              
                try
                {
                   
                    if (File.Exists(path))
                    {
                        // Try UTF-8 first (new format from C++ provider)
                        string userId = File.ReadAllText(path, Encoding.UTF8).Trim();
                        _logger.LogDebug("Raw user ID (UTF8): '{UserId}'", userId);

                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogInformation("User ID successfully read: {UserId}", userId);
                           
                            return userId;
                        }

                        // Fallback: UTF-16LE
                        byte[] raw = File.ReadAllBytes(path);
                        if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE)
                        {
                            userId = Encoding.Unicode.GetString(raw).Trim();
                            _logger.LogDebug("Raw user ID (UTF16 with BOM): '{UserId}'", userId);
                        }
                        else
                        {
                            userId = Encoding.Unicode.GetString(raw).Trim();
                            _logger.LogDebug("Raw user ID (UTF16 no BOM): '{UserId}'", userId);
                        }

                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogInformation("User ID successfully read (fallback): {UserId}", userId);
                            return userId;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("User ID file not found at {Path} (attempt {Attempt}/5)", path, attempt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading UserId file (attempt {Attempt}/5)", attempt);
                }

                if (attempt < 5)
                    Thread.Sleep(2000);
            }

            _logger.LogError("Failed to obtain a valid UserId after 5 attempts.");
            return null;
        }
        private string? GetCurrentDeviceName()
        {
            if (!string.IsNullOrWhiteSpace(cachedDeviceName) &&
                cachedDeviceNameExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Using cached computer name: {DeviceName}", cachedDeviceName);
                return cachedDeviceName;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EnterpriseTrackingAgent",
                "current_device.txt");

            _logger.LogInformation("Attempting to read computer name from: {Path}", path);

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string deviceName = File.ReadAllText(path, Encoding.UTF8).Trim();
                        _logger.LogDebug("Raw computer name (UTF8): '{DeviceName}'", deviceName);

                        if (!string.IsNullOrWhiteSpace(deviceName))
                        {
                            cachedDeviceName = deviceName;
                            cachedDeviceNameExpiresAt = DateTime.UtcNow.Add(DeviceNameCacheDuration);

                            _logger.LogInformation(
                                "Computer name successfully read and cached until {ExpiresAt}: {DeviceName}",
                                cachedDeviceNameExpiresAt,
                                cachedDeviceName);

                            return cachedDeviceName;
                        }

                        byte[] raw = File.ReadAllBytes(path);

                        deviceName = Encoding.Unicode.GetString(raw).Trim();
                        _logger.LogDebug("Raw computer name (UTF16 fallback): '{DeviceName}'", deviceName);

                        if (!string.IsNullOrWhiteSpace(deviceName))
                        {
                            cachedDeviceName = deviceName;
                            cachedDeviceNameExpiresAt = DateTime.UtcNow.Add(DeviceNameCacheDuration);

                            _logger.LogInformation(
                                "Computer name successfully read and cached until {ExpiresAt}: {DeviceName}",
                                cachedDeviceNameExpiresAt,
                                cachedDeviceName);

                            return cachedDeviceName;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Computer name file not found at {Path} (attempt {Attempt}/5)", path, attempt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading computer name file (attempt {Attempt}/5)", attempt);
                }

                if (attempt < 5)
                {
                    Thread.Sleep(2000);
                }
            }

            if (!string.IsNullOrWhiteSpace(cachedDeviceName))
            {
                _logger.LogWarning("Using previously cached computer name after file read failure: {DeviceName}", cachedDeviceName);
                return cachedDeviceName;
            }

            _logger.LogError("Failed to obtain a valid computer name after 5 attempts.");
            return null;
        }

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += async (s, e) =>
            {
                if (!isRunning)
                    await Run();
            };
            timer.Start();

            _ = Task.Run(ProcessQueue);

            _logger.LogInformation("Agent started.");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;

            timer?.Stop();
            _logger.LogInformation("Agent stopped.");
        }

        private async Task Run()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;

                var active = window.GetActiveWindow();
                string? userId = GetCurrentUserId();

                if (currentApp == null)
                {
                    currentApp = active.app;
                    currentTitle = active.title;
                    start = DateTime.UtcNow;
                    _logger.LogInformation("Initial activity: App={App}, Title={Title}, UserId={UserId}",
                        currentApp, currentTitle, userId ?? "NULL");
                    return;
                }

                if (active.app != currentApp || active.title != currentTitle)
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var deviceName = GetCurrentDeviceName() ?? Environment.MachineName;

                        var ev = new ActivityEvent
                        {
                            UserId = userId,
                            MachineId = deviceName,
                            AppName = currentApp,
                            WindowTitle = currentTitle,
                            StartTime = start,
                            EndTime = DateTime.UtcNow,
                            DurationSeconds = (int)(DateTime.UtcNow - start).TotalSeconds,
                            EventType = meeting.IsMeetingApp(currentApp.ToLower())
                                ? "Meeting"
                                : learning.IsLearningCourse(currentTitle.ToLower())
                                    ? "Learning"
                                    : "AppUsage",
                            IsIdle = idle.GetIdleSeconds() > 300
                        };

                        _logger.LogInformation("Activity Event: {Event}", JsonConvert.SerializeObject(ev));

                        bool sent = await sender.SendAsync(ev);
                        if (!sent)
                        {
                            await queue.SaveAsync(ev);
                            _logger.LogWarning("Event queued offline.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Skipping event because UserId is null.");
                    }

                    currentApp = active.app;
                    currentTitle = active.title;
                    start = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Run error");
            }
            finally
            {
                isRunning = false;
            }
        }

        private async Task ProcessQueue()
        {
            while (IsRunning)
            {
                try
                {
                    var items = await queue.GetAllAsync();
                    foreach (var item in items)
                    {
                        var payload = System.Text.Json.JsonSerializer
                            .Deserialize<ActivityEvent>(item.Payload);

                        if (payload == null) continue;

                        bool sent = await sender.SendAsync(payload);
                        if (sent)
                        {
                            await queue.DeleteAsync(item.Id);
                            _logger.LogInformation("Flushed queued event {Id}", item.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Queue processing error");
                }

                await Task.Delay(10000);
            }
        }
    }
}