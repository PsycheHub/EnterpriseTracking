using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Voucher;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IVoucherService
    {
        Task<ResponseDto<string>> CreateVoucher(int validatyDays);
        Task<ResponseDto<string>> ExtendVoucherLifcycle(string voucherId, int validatyDays);
        Task<ResponseDto<string>> DeleteVoucher(string voucherId);
        Task<ResponseDto<string>> LinkVoucher(string voucherId, string userId);
        Task<ResponseDto<string>> UnLinkVoucher(string voucherId);
        Task<ResponseDto<PaginatedResponseDto<VoucherRespDto>>> PaginateVoucher(
      int pageNumber,
      int perPageSize ,
      string? voucherCodeOrUsername,
      string? status);
        Task<ResponseDto<VoucherMetricsDto>> GetVoucherMetrics();
        Task<byte[]> ExportVouchersToExcel(
            DateTime? fromDate,
            DateTime? toDate,
            string? status);

    }
}
