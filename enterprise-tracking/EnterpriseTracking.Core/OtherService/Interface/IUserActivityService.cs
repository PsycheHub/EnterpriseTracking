using EnterpriseTracking.Core.Dto.Request.ComputerActivity;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.ComputerActivity;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using Microsoft.AspNetCore.Http;

namespace EnterpriseTracking.Core.OtherService.Interface
{
    public interface IUserActivityService
    {
        Task<ResponseDto<string>> CreateUserActivity(CreateUserActivityReqDto req);

        Task<ResponseDto<string>> CreateActivityCategory(CreateActivityCategoryReqDto req);

        Task<ResponseDto<string>> UpdateActivityCategory(UpdateActivityCategoryReqDto req);

        Task<ResponseDto<PaginatedResponseDto<ActivityCategoryRespDto>>> PaginateActivityCategories(
            int pageNumber,
            int perPageSize,
            string? search);
        Task<ResponseDto<PaginatedResponseDto<UserActivityListDto>>> PaginateUserActivity(
    int pageNumber,
    int perPageSize,
    string? search);

        Task<ResponseDto<string>> CreateMapAppCategory(CreateMapAppCategoryReqDto req);

        Task<ResponseDto<string>> UpdateMapAppCategory(UpdateMapAppCategoryReqDto req);

        Task<ResponseDto<MapAppCategoryRespDto>> GetMapAppCategoryById(string id);

        Task<ResponseDto<string>> DeleteMapAppCategory(string id);

        Task<ResponseDto<PaginatedResponseDto<MapAppCategoryRespDto>>> PaginateMapAppCategories(
            int pageNumber,
            int perPageSize,
            string catid,
            string? search);
        Task<ResponseDto<string>> UploadProofFile(IFormFile file);
        Task<ResponseDto<ActivityMetricsDto>> GetActivityMetricCard(
      string? userId = null,
      DateTime? fromDate = null,
      DateTime? toDate = null);
        Task<ResponseDto<ActivityBreakdownDto>> GetActivityBreakdown(
      string? userId = null,
      DateTime? fromDate = null,
      DateTime? toDate = null);
        Task<ResponseDto<List<ActivityTrendDto>>> GetActivityTrend(
      TrendRange range,
      string? userId = null);
        Task<ResponseDto<PaginatedResponseDto<UserTopActivityDto>>>
    PaginateUserAppActivities(
        string userId,
        int pageNumber,
        int perPageSize);
        Task<ResponseDto<overallUserDataResp>> GetUserAppBarchartActivities(
    string? userId = null,
    string? appName = null,
    string? categoryId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null);
        Task<ResponseDto<PaginatedResponseDto<ProofOfActivityRespDto>>> PaginateProofUserActivities(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate);
        Task<ResponseDto<PaginatedResponseDto<ProofImageDataReponseDto>>> PaginateProofUserActivitiesImageData(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate);
        Task<ResponseDto<ProofOfActivity>> GetDocFile(string FileIdentifier);
        Task<ResponseDto<DashboardMetricsDto>> GetDashboardMetrics(TrendRange range);
        Task<ResponseDto<ActivityInsightMetricsDto>> GetActivityInsightMetrics(TrendRange range, string? userId = null);
        Task<byte[]> ExportComputerActivities(
    string? userId = null,
    string? appName = null,
    string? categoryId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    TrendRange? range = null);
        Task<ResponseDto<ComputerDeviceMetricsDto>> GetComputerDeviceMetrics(TrendRange range);
        Task<ResponseDto<PaginatedResponseDto<ComputerDeviceListDto>>> PaginateComputerDevices(
            int pageNumber,
            int perPageSize,
            TrendRange? range = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<ResponseDto<List<ComputerDeviceUserUsageDto>>> GetComputerDeviceUserUsage(
            string machineId,
            TrendRange? range = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<byte[]> ExportComputerDeviceReport(
            TrendRange? range = null,
            string? search = null,
            string? machineId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

    }
}





