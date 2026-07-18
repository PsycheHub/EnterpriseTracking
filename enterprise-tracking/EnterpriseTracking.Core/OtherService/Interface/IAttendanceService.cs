using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Attendance;
using EnterpriseTracking.Core.Dto.Response.ComputerActivity;
using EnterpriseTracking.Core.Enum;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IAttendanceService
    {
        Task<ResponseDto<AttendanceReponseDto>> GetAttendanceMetrics(
   string? userId = null,
   DateTime? fromDate = null,
   DateTime? toDate = null);
        Task<ResponseDto<PaginatedResponseDto<AttendanceRecordDto>>> PaginateAttendanceRecords(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    DateTime? startDate,
    DateTime? endDate);
        Task<ResponseDto<List<ActivityTrendDto>>> GetAttendanceTrend(
    TrendRange range,
    string? userId = null);
        Task<byte[]> ExportAttendanceRecords(
    string? userId,
    string? search,
    DateTime? startDate,
    DateTime? endDate,
    TrendRange? range);
    }
}

