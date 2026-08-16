using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers
{
    /// <summary>Attendance records, trend analytics, and export.</summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AttendanceRead")]
    [Route("api/attendance")]
    [ApiController]
    public class AttendanceController : BaseController
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }


        [HttpGet("metrics/trend")]
        public async Task<IActionResult> GetAttendanceTrendAsync(TrendRange range, string? userId)
        {
            var result = await _attendanceService.GetAttendanceTrend(range, userId);

            return HandleResponse(result);
        }

        [HttpGet("dashboard/card")]
        public async Task<IActionResult> GetActivityMetricChart(string? userId = null,
   DateTime? fromDate = null,
   DateTime? toDate = null)
        {
            var result = await _attendanceService.GetAttendanceMetrics(userId, fromDate, toDate);

            return HandleResponse(result);
        }

        [HttpGet("record/paginate")]
        public async Task<IActionResult> PaginateAttendanceRecordsAysnc(
          int pageNumber,
  int perPageSize,
  string? userId,
  string? search,

  DateTime? startDate,
  DateTime? endDate)
        {
            var result = await _attendanceService.PaginateAttendanceRecords(
              pageNumber,
    perPageSize,
    userId,
    search,
  
    startDate,
    endDate);

            return HandleResponse(result);
        }
        [HttpGet("export")]
        public async Task<IActionResult> ExportAttendanceRecords(
            [FromQuery] string? userId,
            [FromQuery] string? search,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] TrendRange? range)
        {
            var fileBytes = await _attendanceService.ExportAttendanceRecords(
                userId,
                search,
                startDate,
                endDate,
                range);

            var fileName = $"attendance-records-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
