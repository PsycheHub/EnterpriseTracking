using EnterpriseTracking.Api.Controllers.Base;
using EnterpriseTracking.Core.Dto.Request.ComputerActivity;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers
{

    [Route("api/activity")]
    public class UserActivityController : BaseController
    {
        private readonly IUserActivityService _activityService;

        public UserActivityController(IUserActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> CreateUserActivity([FromBody] CreateUserActivityReqDto req)
        {
            var result = await _activityService.CreateUserActivity(req);
            return HandleResponse(result);
        }

        [HttpPost("category/create")]
        public async Task<IActionResult> CreateActivityCategory([FromBody] CreateActivityCategoryReqDto req)
        {
            var result = await _activityService.CreateActivityCategory(req);
            return HandleResponse(result);
        }

        [HttpPut("category/update")]
        public async Task<IActionResult> UpdateActivityCategory([FromBody] UpdateActivityCategoryReqDto req)
        {
            var result = await _activityService.UpdateActivityCategory(req);
            return HandleResponse(result);
        }

        [HttpGet("category/list")]
        public async Task<IActionResult> PaginateActivityCategories(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,
            [FromQuery] string? search)
        {
            var result = await _activityService.PaginateActivityCategories(
                pageNumber,
                perPageSize,
                search);

            return HandleResponse(result);
        }
        [HttpGet("dashboard/metrics/top")]
        public async Task<IActionResult> GetActivityMetricChart(string? userId = null,
     DateTime? fromDate = null,
     DateTime? toDate = null)
        {
            var result = await _activityService.GetActivityMetricCard(userId, fromDate, toDate);

            return HandleResponse(result);
        }
        [HttpGet("dashboard/chart/metrics" 
           )]
        public async Task<IActionResult> GetActivityBreakdownChart(string? userId = null,
  DateTime? fromDate = null,
  DateTime? toDate = null)
        {
            var result = await _activityService.GetActivityBreakdown(userId, fromDate, toDate);

            return HandleResponse(result);
        }
        [HttpGet("dashboard/metrics/trend")]
        public async Task<IActionResult> GetActivityTrend(TrendRange range, string? userId)
        {
            var result = await _activityService.GetActivityTrend(range, userId);

            return HandleResponse(result);
        }

        [HttpPost("app-category/create")]
        public async Task<IActionResult> CreateMapAppCategory([FromBody] CreateMapAppCategoryReqDto req)
        {
            var result = await _activityService.CreateMapAppCategory(req);
            return HandleResponse(result);
        }

        [HttpPut("app-category/update")]
        public async Task<IActionResult> UpdateMapAppCategory([FromBody] UpdateMapAppCategoryReqDto req)
        {
            var result = await _activityService.UpdateMapAppCategory(req);
            return HandleResponse(result);
        }

        [HttpGet("app-category/{id}")]
        public async Task<IActionResult> GetMapAppCategoryById(string id)
        {
            var result = await _activityService.GetMapAppCategoryById(id);
            return HandleResponse(result);
        }
        [HttpPost("upload-screenshot")]
        public async Task<IActionResult> UploadProofOfActivity(IFormFile File)
        {
            var result = await _activityService.UploadProofFile(File);
            return HandleResponse(result);
        }

        [HttpDelete("app-category/{id}")]
        public async Task<IActionResult> DeleteMapAppCategory(string id)
        {
            var result = await _activityService.DeleteMapAppCategory(id);
            return HandleResponse(result);
        }

        [HttpGet("app-category/list")]
        public async Task<IActionResult> PaginateMapAppCategories(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,
            [FromQuery] string catid,
            [FromQuery] string? search)
        {
            var result = await _activityService.PaginateMapAppCategories(
                pageNumber,
                perPageSize,
                catid,
                search);

            return HandleResponse(result);
        }
        [HttpGet("user/rank")]
        public async Task<IActionResult> PaginateUserActivity(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,

            [FromQuery] string? search)
        {
            var result = await _activityService.PaginateUserActivity(
                pageNumber,
                perPageSize,

                search);

            return HandleResponse(result);
        }
        [HttpGet("user/app/activities")]
        public async Task<IActionResult> PaginateUserAppActivity(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,

            [FromQuery] string userid)
        {
            var result = await _activityService.PaginateUserAppActivities(
               userid, pageNumber,
                perPageSize);

            return HandleResponse(result);
        }
        [HttpGet("app/bar/all")]
        public async Task<IActionResult> GetUserAppBarchartActivities(
             string? userId = null,
    string? appName = null,
    string? categoryId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null)
        {
            var result = await _activityService.GetUserAppBarchartActivities(
                userId, appName, categoryId, fromDate, toDate);

            return HandleResponse(result);
        }
        [HttpGet("app/proof")]
        public async Task<IActionResult> PaginateProofUserActivitiesAsync(
            int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate)
        {
            var result = await _activityService.PaginateProofUserActivities(
              pageNumber, 
    perPageSize,
    userId,
    search,
    categoryId,
    startDate,
    endDate);

            return HandleResponse(result);
        }
        [HttpGet("app/proof/data/image")]
        public async Task<IActionResult> PaginateProofUserActivitiesImageDataAsync(
            int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate)
        {
            var result = await _activityService.PaginateProofUserActivitiesImageData(
              pageNumber,
    perPageSize,
    userId,
    search,
    categoryId,
    startDate,
    endDate);

            return HandleResponse(result);
        }

        [HttpGet("file")]
        public async Task<IActionResult> GetImageFile(string FileIdentifier)
        {



            var result = await _activityService
                .GetDocFile(FileIdentifier);
            if (result.StatusCode == 200)
            {
                return File(result.Result.ImageData, result.Result.ContentType, result.Result.FileName);
            }
            else if (result.StatusCode == 404)
            {
                return NotFound(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        [HttpGet("dashboard/metrics/summary")]
        public async Task<IActionResult> GetDashboardMetrics([FromQuery] TrendRange range)
        {
            var result = await _activityService.GetDashboardMetrics(range);
            return HandleResponse(result);
        }
        [HttpGet("dashboard/metrics/insights")]
        public async Task<IActionResult> GetActivityInsightMetrics([FromQuery] TrendRange range, [FromQuery] string? userId = null)
        {
            var result = await _activityService.GetActivityInsightMetrics(range, userId);
            return HandleResponse(result);
        }
        [HttpGet("export")]
        public async Task<IActionResult> ExportComputerActivities(
            [FromQuery] string? userId = null,
            [FromQuery] string? appName = null,
            [FromQuery] string? categoryId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] TrendRange? range = null)
        { 
            var fileBytes = await _activityService.ExportComputerActivities(
                userId,
                appName,
                categoryId,
                fromDate,
                toDate,
                range);

            var fileName = $"computer-activities-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        [HttpGet("computer-device/metrics")]
        public async Task<IActionResult> GetComputerDeviceMetrics([FromQuery] TrendRange range)
        {
            var result = await _activityService.GetComputerDeviceMetrics(range);
            return HandleResponse(result);
        }

        [HttpGet("computer-device/list")]
        public async Task<IActionResult> PaginateComputerDevices(
            [FromQuery] int pageNumber,
            [FromQuery] int perPageSize,
            [FromQuery] TrendRange? range = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _activityService.PaginateComputerDevices(
                pageNumber,
                perPageSize,
                range,
                search,
                fromDate,
                toDate);

            return HandleResponse(result);
        }

        [HttpGet("computer-device/{machineId}/users")]
        public async Task<IActionResult> GetComputerDeviceUserUsage(
            [FromRoute] string machineId,
            [FromQuery] TrendRange? range = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _activityService.GetComputerDeviceUserUsage(
                machineId,
                range,
                fromDate,
                toDate);

            return HandleResponse(result);
        }

        [HttpGet("computer-device/export")]
        public async Task<IActionResult> ExportComputerDeviceReport(
            [FromQuery] TrendRange? range = null,
            [FromQuery] string? search = null,
            [FromQuery] string? machineId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var fileBytes = await _activityService.ExportComputerDeviceReport(
                range,
                search,
                machineId,
                fromDate,
                toDate);

            var fileName = $"computer-device-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
