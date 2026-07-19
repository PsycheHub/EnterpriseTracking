using EnterpriseTrackingActivityAgent.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EnterpriseTrackingActivityAgent
{
    public class AgentService
    {
        private readonly ILogger<AgentService> _logger;
        private readonly SessionManager _sessionManager;

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

        public AgentService(ILogger<AgentService> logger, ApiSender sender, SessionManager sessionManager)
        {
            _logger = logger;
            this.sender = sender;
            _sessionManager = sessionManager;
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
                string? userId = _sessionManager.CurrentUserId;

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
                        var ev = new ActivityEvent
                        {
                            UserId = userId,
                            MachineId = Environment.MachineName,
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
                        _logger.LogWarning("Skipping event because UserId is null (no active session).");
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