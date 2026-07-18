namespace EnterpriseTrackingActivityAgent.Services
{


    public class MeetingDetector
    {
        static readonly string[] meetingApps =
        {
        "zoom",
        "teams",
        "ms-teams"
    };

        public bool IsMeetingApp(string process)
        {
            return meetingApps.Contains(process.ToLower());
        }
    }
}
