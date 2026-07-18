namespace EnterpriseTracking.Core.Dto.Request.Voucher
{
    public class ExportVoucherDto
    {
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? status { get; set; }
    }
}
