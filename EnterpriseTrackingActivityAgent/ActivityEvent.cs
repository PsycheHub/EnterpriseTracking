namespace EnterpriseTrackingActivityAgent
{
    public class ActivityEvent
    {
       

        public string UserId { get; set; }
        public string MachineId { get; set; }

        public string AppName { get; set; }
        public string WindowTitle { get; set; }

        public string EventType { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int DurationSeconds { get; set; }

        public bool IsIdle { get; set; }
    }
}
