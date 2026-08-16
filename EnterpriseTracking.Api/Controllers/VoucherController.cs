using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Dto.Request.Voucher;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Infrastructure.OtherService.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers
{
    /// <summary>Voucher lifecycle management — create, link, extend, and delete agent access vouchers.</summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/voucher")]
    [ApiController]
    public class VoucherController : BaseController
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateVoucherAsync([FromQuery] int validatyDays)
        {
            var result = await _voucherService.CreateVoucher(validatyDays);
            return HandleResponse(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("lifecycle/extend")]
        public async Task<IActionResult> ExtendVoucherLifcycleAsync(
            [FromQuery] string voucherId,
            [FromQuery] int validatyDays)
        {
            var result = await _voucherService.ExtendVoucherLifcycle(voucherId, validatyDays);
            return HandleResponse(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteVoucherAsync([FromQuery] string voucherId)
        {
            var result = await _voucherService.DeleteVoucher(voucherId);
            return HandleResponse(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("link")]
        public async Task<IActionResult> LinkVoucherAsync(
            [FromQuery] string voucherId,
            [FromQuery] string userId)
        {
            var result = await _voucherService.LinkVoucher(voucherId, userId);
            return HandleResponse(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("unlink")]
        public async Task<IActionResult> UnLinkVoucherAsync([FromQuery] string voucherId)
        {
            var result = await _voucherService.UnLinkVoucher(voucherId);
            return HandleResponse(result);
        }

        [Authorize(Policy = "VouchersRead")]
        [HttpGet("list")]
        public async Task<IActionResult> PaginateVoucherAsync(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,
            [FromQuery] string? voucherCodeOrUsername,
            [FromQuery] string? status)
        {
            var result = await _voucherService.PaginateVoucher(
                pageNumber,
                perPageSize,
                voucherCodeOrUsername,
                status);

            return HandleResponse(result);
        }

        [Authorize(Policy = "VouchersRead")]
        [HttpGet("metrics")]
        public async Task<IActionResult> GetVoucherMetricsAsync()
        {
            var result = await _voucherService.GetVoucherMetrics();
            return HandleResponse(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("user/export")]
        public async Task<IActionResult> ExportVoucher(ExportVoucherDto request)
        {
            var file = await _voucherService.ExportVouchersToExcel(request.fromDate, request.toDate,request.status);
            return File(
               file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Voucher_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}