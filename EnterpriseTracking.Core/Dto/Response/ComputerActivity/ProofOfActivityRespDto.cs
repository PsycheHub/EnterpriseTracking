namespace EnterpriseTracking.Core.Dto.Response.ComputerActivity
{
    public class ProofOfActivityRespDto
    {

        public string AppName { get; set; }
        public string Category { get; set; }
        public DateTime Timestamp { get; set; }

        public string? PreviewIdentifier { get; set; }
        public int Count { get; set; }

    }
}
