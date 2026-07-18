using EnterpriseTracking.Core.Dto.Request.ComputerActivity;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.ComputerActivity;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using EnterpriseTracking.Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using ClosedXML.Excel;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class UserActivityService : IUserActivityService
    {
        private readonly ILogger<UserActivityService> _logger;
        private readonly IEnterpriseTrackingGenericRepo<UserActivity> _userActivityRepo;
        private readonly IEnterpriseTrackingGenericRepo<ActivityCategory> _activityCategoryRepo;
        private readonly IEnterpriseTrackingGenericRepo<MapAppCatories> _mapAppCategoryRepo;
        private readonly IEnterpriseTrackingGenericRepo<ProofOfActivity> _proofOfActivityRepo;
        private readonly EnterpriseTrackingContext _context;
        private readonly IEnterpriseTrackingGenericRepo<Voucher> _voucherRepo;
        private readonly IEnterpriseTrackingGenericRepo<Attendance> _attendanceRepo;
        private readonly IHelperServ _helperServ;

        public UserActivityService(ILogger<UserActivityService> logger,
            IEnterpriseTrackingGenericRepo<UserActivity> userActivityRepo,
            IEnterpriseTrackingGenericRepo<ActivityCategory> activityCategoryRepo,
            IEnterpriseTrackingGenericRepo<MapAppCatories> mapAppNameRepo, IHelperServ helperServ,
            IEnterpriseTrackingGenericRepo<ProofOfActivity> proofOfActivityRepo,
            
    IEnterpriseTrackingGenericRepo<Voucher> voucherRepo,
            IEnterpriseTrackingGenericRepo<Attendance> attendanceRepo, EnterpriseTrackingContext context)
        {
            _logger = logger;
            _userActivityRepo = userActivityRepo;
            _activityCategoryRepo = activityCategoryRepo;
            _mapAppCategoryRepo = mapAppNameRepo;
            _helperServ = helperServ;
            _proofOfActivityRepo = proofOfActivityRepo;
            
            _voucherRepo = voucherRepo;
            _attendanceRepo = attendanceRepo;
            _context = context;
        }
        public async Task<ResponseDto<string>> CreateUserActivity(CreateUserActivityReqDto req)
        {
            var response = new ResponseDto<string>();

            try
            {
                var catId = String.Empty;
                var findCategory = await _mapAppCategoryRepo.GetQueryable().
                    FirstOrDefaultAsync(u => u.Name.ToLower() == req.AppName.ToLower()
                    && u.IsDeleted != true);
                if (findCategory == null)
                {
                    var findOtherCat = await _mapAppCategoryRepo.GetQueryable()
                         .FirstOrDefaultAsync(u => u.Name.ToLower() == "others");
                    catId = findOtherCat.Id;
                }
                else
                {
                    catId = findCategory.Id;
                }

                await _userActivityRepo.Add(new UserActivity
                {
                    UserId = req.UserId,
                    MapCategoryId = catId,
                    MachineId = req.MachineId,
                    AppName = req.AppName,
                    WindowTitle = req.WindowTitle,
                    EventType = req.EventType,
                    StartTime = req.StartTime,
                    EndTime = req.EndTime,
                    DurationSeconds = req.DurationSeconds,
                    IsIdle = req.IsIdle
                });
                var utcNow = DateTime.UtcNow;
                var startOfDay = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
                var endOfDay = startOfDay.AddDays(1);
                var checkAttendance = await _attendanceRepo.GetQueryable()
                    .AnyAsync(x =>
            x.UserId == req.UserId &&
            x.Created >= startOfDay &&
            x.Created < endOfDay); 
                if (!checkAttendance)
                {
                    await _attendanceRepo.Add(new Attendance() { UserId = req.UserId });
                }
                await _userActivityRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "User activity recorded successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.ErrorMessages = new List<string>()
        {
            "Error while creating user activity"
        };

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";

                return response;
            }
        }

        public async Task<ResponseDto<string>> CreateActivityCategory(CreateActivityCategoryReqDto req)
        {
            var response = new ResponseDto<string>();

            try
            {
                var category = new ActivityCategory
                {
                    Name = req.Name,
                    Description = req.Description
                };

                await _activityCategoryRepo.Add(category);
                await _activityCategoryRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Activity category created successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error creating activity category" };

                return response;
            }
        }


        public async Task<ResponseDto<string>> UpdateActivityCategory(UpdateActivityCategoryReqDto req)
        {
            var response = new ResponseDto<string>();

            try
            {
                var category = await _activityCategoryRepo.GetByIdAsync(req.Id);

                if (category == null)
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Category not found" };
                    return response;
                }

                category.Name = req.Name;
                category.Description = req.Description;

                _activityCategoryRepo.Update(category);
                await _activityCategoryRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Activity category updated successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error updating activity category" };

                return response;
            }
        }

        public async Task<ResponseDto<PaginatedResponseDto<ActivityCategoryRespDto>>> PaginateActivityCategories(
    int pageNumber,
    int perPageSize,
    string? search)
        {
            var response = new ResponseDto<PaginatedResponseDto<ActivityCategoryRespDto>>();

            try
            {
                var query = _activityCategoryRepo
                                .GetQueryable()
                                .Where(c => !c.IsDeleted)
                                .AsQueryable();

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        c.Name.Contains(search) ||
                        c.Description.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var categories = await query
                    .OrderByDescending(c => c.Created)
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select(c => new ActivityCategoryRespDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        Created = c.Created
                    })
                    .ToListAsync();

                var result = new PaginatedResponseDto<ActivityCategoryRespDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = categories,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string>()
        {
            "Error retrieving activity categories"
        };

                return response;
            }
        }
        public async Task<ResponseDto<string>> CreateMapAppCategory(CreateMapAppCategoryReqDto req)
        {
            var response = new ResponseDto<string>();

            try
            {
                var entity = new MapAppCatories
                {
                    Name = req.Name,
                    IconUrl = req.IconUrl,
                    EventType = req.EventType,
                    CategoryId = req.CategoryId
                };

                await _mapAppCategoryRepo.Add(entity);
                await _mapAppCategoryRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Application category mapped successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error creating mapping" };

                return response;
            }
        }
        public async Task<ResponseDto<string>> UpdateMapAppCategory(UpdateMapAppCategoryReqDto req)
        {
            var response = new ResponseDto<string>();

            try
            {
                var entity = await _mapAppCategoryRepo.GetByIdAsync(req.Id);

                if (entity == null || entity.IsDeleted)
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Mapping not found" };
                    return response;
                }

                entity.Name = req.Name;
                entity.IconUrl = req.IconUrl;
                entity.EventType = req.EventType;
                entity.CategoryId = req.CategoryId;
                entity.DateUpdated = DateTime.UtcNow;

                _mapAppCategoryRepo.Update(entity);
                await _mapAppCategoryRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Mapping updated successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error updating mapping" };

                return response;
            }
        }
        public async Task<ResponseDto<MapAppCategoryRespDto>> GetMapAppCategoryById(string id)
        {
            var response = new ResponseDto<MapAppCategoryRespDto>();

            try
            {
                var entity = await _mapAppCategoryRepo
                                .GetQueryable()
                                .Where(x => x.Id == id && !x.IsDeleted)
                                .Include(x => x.Category)
                                .Select(x => new MapAppCategoryRespDto
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    IconUrl = x.IconUrl,
                                    EventType = x.EventType,
                                    CategoryId = x.CategoryId,
                                    CategoryName = x.Category.Name,
                                    Created = x.Created
                                })
                                .FirstOrDefaultAsync();

                if (entity == null)
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Mapping not found" };
                    return response;
                }

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = entity;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving mapping" };

                return response;
            }
        }
        public async Task<ResponseDto<string>> DeleteMapAppCategory(string id)
        {
            var response = new ResponseDto<string>();

            try
            {
                var entity = await _mapAppCategoryRepo.GetByIdAsync(id);

                if (entity == null || entity.IsDeleted)
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Mapping not found" };
                    return response;
                }

                entity.IsDeleted = true;
                entity.DateUpdated = DateTime.UtcNow;

                _mapAppCategoryRepo.Update(entity);
                await _mapAppCategoryRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Mapping deleted successfully";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error deleting mapping" };

                return response;
            }
        }
        public async Task<ResponseDto<PaginatedResponseDto<MapAppCategoryRespDto>>> PaginateMapAppCategories(
    int pageNumber,
    int perPageSize,
    string catid,
    string? search)
        {
            var response = new ResponseDto<PaginatedResponseDto<MapAppCategoryRespDto>>();

            try
            {
                var query = _mapAppCategoryRepo
                                .GetQueryable()
                                .Where(x => !x.IsDeleted && x.CategoryId == catid)
                                .Include(x => x.Category)
                                .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        x.Name.Contains(search) ||
                        x.Category.Name.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.Created)
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select(x => new MapAppCategoryRespDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        IconUrl = x.IconUrl,
                        EventType = x.EventType,
                        CategoryId = x.CategoryId,
                        CategoryName = x.Category.Name,
                        Created = x.Created
                    })
                    .ToListAsync();

                var result = new PaginatedResponseDto<MapAppCategoryRespDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = data,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving mappings" };

                return response;
            }
        }

        public async Task<ResponseDto<string>> UploadProofFile(IFormFile file)
        {
            var result = new ResponseDto<string>();
            try
            {


                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                var splitfileName = file.FileName.Split("@");
                var appName = splitfileName[0];
                var timestamp = splitfileName[1];
                var userId = splitfileName[2];
                var identifier = splitfileName[3].Split(".")[0];

                DateTime dateTime = DateTime.ParseExact(
    timestamp,
    "yyyyMMdd_HHmmss",
    System.Globalization.CultureInfo.InvariantCulture,
    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal
);
                var catId = String.Empty;
                var findCategory = await _mapAppCategoryRepo.GetQueryable().
                    FirstOrDefaultAsync(u => u.Name.ToLower() == appName.ToLower()
                    && u.IsDeleted != true);
                if (findCategory == null)
                {
                    var findOtherCat = await _mapAppCategoryRepo.GetQueryable()
                         .FirstOrDefaultAsync(u => u.Name.ToLower() == "others");
                    catId = findOtherCat.Id;
                }
                else
                {
                    catId = findCategory.Id;
                }


                var uploadFile = new ProofOfActivity()
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    ImageData = fileBytes,
                    DocIdentifier = identifier,
                    MapCategoryId = catId,
                    UserId = userId,
                    AppName = appName,
                    Created = dateTime
                };



                await _proofOfActivityRepo.Add(uploadFile);
                await _proofOfActivityRepo.SaveChanges();
                result.StatusCode = 200;
                result.Result = identifier;
                result.DisplayMessage = "Success";
                return result;
            }
            catch (Exception ex)
            {
                Activity.Current?.SetTag(ex.Message, JsonConvert.SerializeObject(ex));
                result.DisplayMessage = "error";
                result.ErrorMessages = new List<string>() { "Get proof of activity service unavailable, please try again later" };
                result.StatusCode = 400;
                return result;
            }
        }

        public async Task<ResponseDto<ActivityMetricsDto>> GetActivityMetricCard(
      string? userId = null,
      DateTime? fromDate = null,
      DateTime? toDate = null)
        {
            var response = new ResponseDto<ActivityMetricsDto>();

            try
            {
                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted);

                // =========================
                // 🔹 OPTIONAL FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    query = query.Where(x => x.UserId == userId);
                }

                // =========================
                // 🔹 OPTIONAL FILTER: DATE RANGE
                // =========================
                if (fromDate.HasValue)
                {
                    var from = fromDate.Value.Date;
                    query = query.Where(x => x.Created >= from);
                }

                if (toDate.HasValue)
                {
                    // inclusive end of day
                    var to = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.Created < to);
                }

                // =========================
                // 🔹 TOTAL ACTIVITY (non-idle)
                // =========================
                var totalActivitySeconds = await query
                    .Where(x => !x.IsIdle)
                    .SumAsync(x => (int?)x.DurationSeconds) ?? 0;

                // =========================
                // 🔹 TOTAL IDLE
                // =========================
                var totalIdleSeconds = await query
                    .Where(x => x.IsIdle)
                    .SumAsync(x => (int?)x.DurationSeconds) ?? 0;

                // =========================
                // 🔹 AVERAGE DAILY USAGE
                // =========================
                var dailyUsage = await query
                    .Where(x => !x.IsIdle)
                    .GroupBy(x => x.Created.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .ToListAsync();

                var averageDailySeconds = dailyUsage.Any()
                    ? (int)dailyUsage.Average(x => x.TotalSeconds)
                    : 0;

                // =========================
                // 🔹 RESPONSE
                // =========================
                var metrics = new ActivityMetricsDto
                {
                    TotalActivityTime = _helperServ.FormatTime(totalActivitySeconds),
                    AverageDailyUsage = _helperServ.FormatTime(averageDailySeconds),
                    TotalIdleTime = _helperServ.FormatTime(totalIdleSeconds)
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = metrics;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving activity metrics" };

                return response;
            }
        }

        public async Task<ResponseDto<ActivityBreakdownDto>> GetActivityBreakdown(
     string? userId = null,
     DateTime? fromDate = null,
     DateTime? toDate = null)
        {
            var response = new ResponseDto<ActivityBreakdownDto>();

            try
            {
                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && !x.IsIdle); // exclude idle

                // =========================
                // 🔹 OPTIONAL FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    query = query.Where(x => x.UserId == userId);
                }

                // =========================
                // 🔹 OPTIONAL FILTER: DATE RANGE
                // =========================
                if (fromDate.HasValue)
                {
                    var from = fromDate.Value.Date;
                    query = query.Where(x => x.Created >= from);
                }

                if (toDate.HasValue)
                {
                    var to = toDate.Value.Date.AddDays(1); // inclusive end date
                    query = query.Where(x => x.Created < to);
                }

                // =========================
                // 🔹 TOP APPS & SITES
                // =========================
                var topApps = await query
                    .GroupBy(x => x.AppName)
                    .Select(g => new ChartItemDto
                    {
                        Name = g.Key ?? "Unknown",
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .Take(10)
                    .ToListAsync();

                // =========================
                // 🔹 TOP CATEGORIES
                // =========================
                var topCategories = await query
                    .GroupBy(x => x.MapCategory.Category.Name)
                    .Select(g => new ChartItemDto
                    {
                        Name = g.Key ?? "No Category",
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .Take(10)
                    .ToListAsync();

                // =========================
                // 🔹 FORMAT TIME (AFTER QUERY)
                // =========================
                topApps.ForEach(x =>
                    x.FormattedTime = _helperServ.FormatTime(x.TotalSeconds));

                topCategories.ForEach(x =>
                    x.FormattedTime = _helperServ.FormatTime(x.TotalSeconds));

                // =========================
                // 🔹 RESPONSE
                // =========================
                var result = new ActivityBreakdownDto
                {
                    TopApps = topApps,
                    TopCategories = topCategories
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving activity breakdown" };

                return response;
            }
        }
        public async Task<ResponseDto<List<ActivityTrendDto>>> GetActivityTrend(
     TrendRange range,
     string? userId = null)
        {
            var response = new ResponseDto<List<ActivityTrendDto>>();

            try
            {
                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && !x.IsIdle);

                // =========================
                // 🔹 OPTIONAL FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    query = query.Where(x => x.UserId == userId);
                }

                var today = DateTime.UtcNow.Date;

                List<ActivityTrendDto> result;

                switch (range)
                {
                    // =========================
                    // 🔹 LAST 7 DAYS
                    // =========================
                    case TrendRange.Last7Days:
                        {
                            var startDate = DateTime.SpecifyKind(today.AddDays(-6), DateTimeKind.Utc);

                            var data = await query
                                .Where(x => x.Created >= startDate)
                                .GroupBy(x => x.Created.Date)
                                .Select(g => new
                                {
                                    Date = g.Key,
                                    TotalSeconds = g.Sum(x => x.DurationSeconds)
                                })
                                .ToListAsync();

                            result = Enumerable.Range(0, 7)
                                .Select(i =>
                                {
                                    var date = startDate.AddDays(i);
                                    var match = data.FirstOrDefault(x => x.Date == date);

                                    var seconds = match?.TotalSeconds ?? 0;

                                    return new ActivityTrendDto
                                    {
                                        Label = date.ToString("MMM d"),
                                        TotalSeconds = seconds,
                                        FormattedTime = _helperServ.FormatTime(seconds)
                                    };
                                })
                                .ToList();

                            break;
                        }

                    // =========================
                    // 🔹 LAST 30 DAYS
                    // =========================
                    case TrendRange.Last30Days:
                        {
                            var startDate = DateTime.SpecifyKind(today.AddDays(-29), DateTimeKind.Utc);

                            var data = await query
                                .Where(x => x.Created >= startDate)
                                .GroupBy(x => x.Created.Date)
                                .Select(g => new
                                {
                                    Date = g.Key,
                                    TotalSeconds = g.Sum(x => x.DurationSeconds)
                                })
                                .ToListAsync();

                            result = Enumerable.Range(0, 30)
                                .Select(i =>
                                {
                                    var date = startDate.AddDays(i);
                                    var match = data.FirstOrDefault(x => x.Date == date);

                                    var seconds = match?.TotalSeconds ?? 0;

                                    return new ActivityTrendDto
                                    {
                                        Label = date.ToString("MMM d"),
                                        TotalSeconds = seconds,
                                        FormattedTime = _helperServ.FormatTime(seconds)
                                    };
                                })
                                .ToList();

                            break;
                        }

                    // =========================
                    // 🔹 CURRENT YEAR (MONTHLY)
                    // =========================
                    case TrendRange.CurrentYear:
                        {
                            var startYear = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                            var data = await query
                                .Where(x => x.Created >= startYear)
                                .GroupBy(x => new { x.Created.Year, x.Created.Month })
                                .Select(g => new
                                {
                                    g.Key.Month,
                                    TotalSeconds = g.Sum(x => x.DurationSeconds)
                                })
                                .ToListAsync();

                            result = Enumerable.Range(1, 12)
                                .Select(month =>
                                {
                                    var match = data.FirstOrDefault(x => x.Month == month);
                                    var seconds = match?.TotalSeconds ?? 0;

                                    var date = new DateTime(today.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);

                                    return new ActivityTrendDto
                                    {
                                        Label = date.ToString("MMM"),
                                        TotalSeconds = seconds,
                                        FormattedTime = _helperServ.FormatTime(seconds)
                                    };
                                })
                                .ToList();

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(range), "Invalid range");
                }

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving activity trend" };

                return response;
            }
        }

        public async Task<ResponseDto<PaginatedResponseDto<UserActivityListDto>>> PaginateUserActivity(
     int pageNumber,
     int perPageSize,
     string? search)
        {
            var response = new ResponseDto<PaginatedResponseDto<UserActivityListDto>>();

            try
            {
                var activityQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted);

                // 🔍 Search by username
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    activityQuery = activityQuery
                        .Where(x => x.User.UserName.ToLower().Contains(search));
                }

                // =========================
                // 🔹 GROUP BY USER
                // =========================
                var grouped = await activityQuery
                    .GroupBy(x => new
                    {
                        x.UserId,
                        x.User.UserName,
                        x.User.FirstName,
                        x.User.LastName
                    })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        UserName = g.Key.UserName,
                        FullName = g.Key.FirstName + " " + g.Key.LastName,

                        TotalActivitySeconds = g
                            .Where(x => !x.IsIdle)
                            .Sum(x => x.DurationSeconds),

                        TotalIdleSeconds = g
                            .Where(x => x.IsIdle)
                            .Sum(x => x.DurationSeconds),

                        ActiveDays = g
                            .Where(x => !x.IsIdle)
                            .Select(x => x.Created.Date)
                            .Distinct()
                            .Count(),

                        IdleDays = g
                            .Where(x => x.IsIdle)
                            .Select(x => x.Created.Date)
                            .Distinct()
                            .Count(),

                        LastActive = g.Max(x => x.Created)
                    })
                    .ToListAsync();

                // =========================
                // 🔹 PROOF COUNT
                // =========================
                var userIds = grouped.Select(x => x.UserId).ToList();

                var proofCounts = await _proofOfActivityRepo
                    .GetQueryable()
                    .Where(p => userIds.Contains(p.UserId) && !p.IsDeleted)
                    .GroupBy(p => p.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                // =========================
                // 🔹 FINAL PROJECTION + RANK
                // =========================
                var rankedList = grouped
                    .OrderByDescending(x => x.TotalActivitySeconds)
                    .Select((x, index) =>
                    {
                        var avgDaily = x.ActiveDays > 0
                            ? x.TotalActivitySeconds / x.ActiveDays
                            : 0;

                        var avgIdle = x.IdleDays > 0
                            ? x.TotalIdleSeconds / x.IdleDays
                            : 0;

                        proofCounts.TryGetValue(x.UserId, out var proofCount);

                        return new UserActivityListDto
                        {
                            UserId = x.UserId,
                            FullName = x.FullName,
                            UserName = x.UserName,
                            Rank = index + 1,

                            TotalActivityTime = _helperServ.FormatTime(x.TotalActivitySeconds),
                            AvgDailyTime = _helperServ.FormatTime(avgDaily),
                            AvgIdleTime = _helperServ.FormatTime(avgIdle),

                            ActivityProofCount = proofCount,
                            LastActive = _helperServ.FormatLastActive(x.LastActive)
                        };
                    })
                    .ToList();

                var totalCount = rankedList.Count;

                var pagedData = rankedList
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .ToList();

                var result = new PaginatedResponseDto<UserActivityListDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    TotalCount = totalCount,
                    Data = pagedData
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving user activity list" };

                return response;
            }
        }


        public async Task<ResponseDto<PaginatedResponseDto<UserTopActivityDto>>> PaginateUserAppActivities(
        string userId,
        int pageNumber,
        int perPageSize)
        {
            var response = new ResponseDto<PaginatedResponseDto<UserTopActivityDto>>();

            try
            {
                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted &&
                                !x.IsIdle &&
                                x.UserId == userId)
                    .Include(x => x.MapCategory)
                    .AsQueryable();

                // =========================
                // 🔹 GROUP BY APP
                // =========================
                var grouped = await query
                    .GroupBy(x => new { x.AppName, x.MapCategory.Category.Name })
                    .Select(g => new
                    {
                        AppName = g.Key.AppName,
                        Category = g.Key.Name ?? "No Category",
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .ToListAsync();

                // 🔹 TOTAL TIME (for percentage)
                var totalTime = grouped.Sum(x => x.TotalSeconds);

                // =========================
                // 🔹 PROOF COUNT
                // =========================
                var appNames = grouped.Select(x => x.AppName).ToList();

                var proofCounts = await _proofOfActivityRepo
                    .GetQueryable()
                    .Where(p => p.UserId == userId &&
                                appNames.Contains(p.AppName) &&
                                !p.IsDeleted)
                    .GroupBy(p => p.AppName)
                    .Select(g => new
                    {
                        AppName = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.AppName, x => x.Count);

                // =========================
                // 🔹 FINAL DATA + RANK
                // =========================
                var ranked = grouped
                    .OrderByDescending(x => x.TotalSeconds)
                    .Select((x, index) =>
                    {
                        proofCounts.TryGetValue(x.AppName, out var proofCount);

                        var percentage = totalTime > 0
                            ? (double)x.TotalSeconds / totalTime * 100
                            : 0;

                        return new UserTopActivityDto
                        {
                            No = index + 1,
                            AppName = x.AppName,
                            Category = x.Category,
                            ActivityProofCount = proofCount,
                            TimeSpent = _helperServ.FormatTime(x.TotalSeconds),
                            PercentageUsage = Math.Round(percentage, 1)
                        };
                    })
                    .ToList();

                // =========================
                // 🔹 PAGINATION
                // =========================
                var totalCount = ranked.Count;

                var paged = ranked
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .ToList();

                var result = new PaginatedResponseDto<UserTopActivityDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    TotalCount = totalCount,
                    Data = paged
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string>
        {
            "Error retrieving user top activities"
        };

                return response;
            }
        }
        public async Task<ResponseDto<overallUserDataResp>> GetUserAppBarchartActivities(
    string? userId = null,
    string? appName = null,
    string? categoryId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null)
        {
            var response = new ResponseDto<overallUserDataResp>();

            try
            {
                // =========================
                // 🔹 BASE QUERY
                // =========================
                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && !x.IsIdle)
                    .Include(x => x.MapCategory)
                        .ThenInclude(mc => mc.Category)
                    .AsQueryable();

                // =========================
                // 🔹 APPLY FILTERS
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                    query = query.Where(x => x.UserId == userId);

                if (!string.IsNullOrWhiteSpace(appName))
                    query = query.Where(x => x.AppName == appName);

                if (!string.IsNullOrWhiteSpace(categoryId))
                    query = query.Where(x => x.MapCategory.CategoryId == categoryId);

                if (fromDate.HasValue)
                {
                    var fromUtc = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.StartTime >= fromUtc);
                }

                if (toDate.HasValue)
                {
                    var toUtc = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.StartTime <= toUtc);
                }

                // =========================
                // 🔹 GROUP DATA
                // =========================
                var grouped = await query
                    .GroupBy(x => new
                    {
                        x.AppName,
                        CategoryName = x.MapCategory.Category.Name
                    })
                    .Select(g => new
                    {
                        AppName = g.Key.AppName,
                        Category = g.Key.CategoryName ?? "No Category",
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .ToListAsync();

                // =========================
                // 🔹 TOTAL TIME
                // =========================
                var totalTimeSeconds = grouped.Sum(x => x.TotalSeconds);

                // =========================
                // 🔹 BUILD RESPONSE DATA
                // =========================
                var chartData = grouped
                    .OrderByDescending(x => x.TotalSeconds)
                    .Select((x, index) =>
                    {
                        var percentage = totalTimeSeconds > 0
                            ? (double)x.TotalSeconds / totalTimeSeconds * 100
                            : 0;

                        return new ChartUserActivitiesOverall
                        {
                            No = index + 1,
                            AppName = x.AppName,
                            Category = x.Category,
                            TimeSpent = _helperServ.FormatTime(x.TotalSeconds),
                            PercentageUsage = Math.Round(percentage, 1)
                        };
                    })
                    .ToList();

                // =========================
                // 🔹 FINAL RESPONSE
                // =========================
                var result = new overallUserDataResp
                {
                    TotalTimeSpent = _helperServ.FormatTime(totalTimeSeconds),
                    ChartData = chartData
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string>
        {
            "Error retrieving user activity overview"
        };

                return response;
            }
        }


        public async Task<ResponseDto<PaginatedResponseDto<ProofOfActivityRespDto>>> PaginateProofUserActivities(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate)
        {
            var response = new ResponseDto<PaginatedResponseDto<ProofOfActivityRespDto>>();

            try
            {
                var query = _proofOfActivityRepo
                    .GetQueryable()
                    .Include(x => x.MapCategory)
                    .Where(x => !x.IsDeleted)
                    .AsQueryable();

                // ✅ Filters
                if (!string.IsNullOrWhiteSpace(userId))
                    query = query.Where(x => x.UserId == userId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(x => x.AppName.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(categoryId))
                    query = query.Where(x => x.MapCategory.CategoryId == categoryId);

                if (startDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.Created >= startUtc);
                }

                if (endDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.Created <= endUtc);
                }

                // ✅ GROUP ONLY BY APP + CATEGORY (case-insensitive AppName)
                var groupedQuery = query
                    .GroupBy(x => new
                    {
                        AppName = x.AppName.ToLower(), // normalize
                        Category = x.MapCategory.Category.Name
                    })
                    .Select(g => new
                    {
                        AppName = g.First().AppName, // preserve original casing
                        g.Key.Category,
                        Count = g.Count(),

                        // latest activity for ordering + timestamp display
                        LatestCreated = g.Max(x => x.Created),

                        // preview identifier from latest record
                        PreviewIdentifier = g
                            .OrderByDescending(x => x.Created)
                            .Select(x => x.DocIdentifier)
                            .FirstOrDefault()
                    });

                var totalCount = await groupedQuery.CountAsync();

                var data = await groupedQuery
                    .OrderByDescending(x => x.LatestCreated)
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select(x => new ProofOfActivityRespDto
                    {
                        AppName = x.AppName,
                        Category = x.Category,
                        Timestamp = x.LatestCreated, // ✅ show latest activity time
                        Count = x.Count,
                        PreviewIdentifier = x.PreviewIdentifier
                    })
                    .ToListAsync();

                var result = new PaginatedResponseDto<ProofOfActivityRespDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = data,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Activities retrieved successfully";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string>
        {
            "Error retrieving activities"
        };

                return response;
            }
        }
        public async Task<ResponseDto<PaginatedResponseDto<ProofImageDataReponseDto>>> PaginateProofUserActivitiesImageData(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    string? categoryId,
    DateTime? startDate,
    DateTime? endDate)
        {
            var response = new ResponseDto<PaginatedResponseDto<ProofImageDataReponseDto>>();

            try
            {
                var query = _proofOfActivityRepo
                    .GetQueryable()
                    .Include(x => x.MapCategory)
                    .Where(x => !x.IsDeleted)
                    .AsQueryable();

                // ✅ Filters
                if (!string.IsNullOrWhiteSpace(userId))
                    query = query.Where(x => x.UserId == userId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(x => x.AppName.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(categoryId))
                    query = query.Where(x => x.MapCategory.CategoryId == categoryId);

                if (startDate.HasValue)
                {
                    var startUtc = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.Created >= startUtc);
                }

                if (endDate.HasValue)
                {
                    var endUtc = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                    query = query.Where(x => x.Created <= endUtc);
                }



                var totalCount = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.Created)
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select(x => new ProofImageDataReponseDto
                    {
                        AppName = x.AppName,
                        Category = x.MapCategory.Category.Name,
                        Timestamp = x.Created,
                        ImageIdentifier = x.DocIdentifier
                    })
                    .ToListAsync();

                var result = new PaginatedResponseDto<ProofImageDataReponseDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = data,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Activities retrieved successfully";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string>
        {
            "Error retrieving activities proof data"
        };

                return response;
            }
        }
        public async Task<ResponseDto<ProofOfActivity>> GetDocFile(string FileIdentifier)
        {
            var result = new ResponseDto<ProofOfActivity>();
            try
            {
                var getDocFile = await _proofOfActivityRepo.GetQueryable().FirstOrDefaultAsync(u => u.DocIdentifier == FileIdentifier);
                if (getDocFile == null)
                {
                    result.ErrorMessages = new List<string>() { "Invalid file requested" };
                    result.StatusCode = 400;
                    result.DisplayMessage = "Error";
                    return result;
                }
                result.StatusCode = 200;
                result.Result = getDocFile;
                result.DisplayMessage = "Success";
                return result;
            }
            catch (Exception ex)
            {
                Activity.Current?.SetTag(ex.Message, JsonConvert.SerializeObject(ex));
                result.DisplayMessage = "error";
                result.ErrorMessages = new List<string>() { "Get image file service unavailable, please try again later" };
                result.StatusCode = 400;
                return result;
            }
        }
        public async Task<ResponseDto<DashboardMetricsDto>> GetDashboardMetrics(TrendRange range)
        {
            var response = new ResponseDto<DashboardMetricsDto>();

            try
            {
                var today = DateTime.UtcNow.Date;

                DateTime startDate;
                DateTime endDate;

                switch (range)
                {
                    case TrendRange.Last7Days:
                        startDate = today.AddDays(-6);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.Last30Days:
                        startDate = today.AddDays(-29);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.CurrentYear:
                        startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = startDate.AddYears(1);
                        break;

                    default:
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        response.DisplayMessage = "Error";
                        response.ErrorMessages = new List<string> { "Invalid trend range supplied" };
                        return response;
                }

                var totalUsers = await _context.Users.Where(u => !u.IsDeleted && u.Created >= startDate && u.Created < endDate).CountAsync();




                var activeVouchers = await _voucherRepo
                    .GetQueryable()
                    .Where(v =>
                        !v.IsDeleted &&
                        v.Status == VoucherStatus.Active.ToString() &&
                        v.Created >= startDate &&
                        v.Created < endDate)
                    .CountAsync();

                var totalAttendance = await _attendanceRepo
                    .GetQueryable()
                    .Where(a =>
                        !a.IsDeleted &&
                        a.Created >= startDate &&
                        a.Created < endDate)
                    .CountAsync();

                var totalActivitySeconds = await _userActivityRepo
                    .GetQueryable()
                    .Where(a =>
                        !a.IsDeleted &&
                        !a.IsIdle &&
                        a.Created >= startDate &&
                        a.Created < endDate)
                    .SumAsync(a => (int?)a.DurationSeconds) ?? 0;

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = new DashboardMetricsDto
                {
                    TotalUsers = totalUsers,
                    ActiveVouchers = activeVouchers,
                    TotalAttendance = totalAttendance,
                    TotalActivityTime = _helperServ.FormatTime(totalActivitySeconds)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard metrics");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving dashboard metrics" };

                return response;
            }
        }
        public async Task<ResponseDto<ActivityInsightMetricsDto>> GetActivityInsightMetrics(TrendRange range, string? userId = null)
        {
            var response = new ResponseDto<ActivityInsightMetricsDto>();

            try
            {
                var today = DateTime.UtcNow.Date;

                DateTime startDate;
                DateTime endDate;
                DateTime previousStartDate;
                DateTime previousEndDate;

                switch (range)
                {
                    case TrendRange.Last7Days:
                        startDate = today.AddDays(-6);
                        endDate = today.AddDays(1);
                        previousStartDate = startDate.AddDays(-7);
                        previousEndDate = startDate;
                        break;

                    case TrendRange.Last30Days:
                        startDate = today.AddDays(-29);
                        endDate = today.AddDays(1);
                        previousStartDate = startDate.AddDays(-30);
                        previousEndDate = startDate;
                        break;

                    case TrendRange.CurrentYear:
                        startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = startDate.AddYears(1);
                        previousStartDate = startDate.AddYears(-1);
                        previousEndDate = startDate;
                        break;

                    default:
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        response.DisplayMessage = "Error";
                        response.ErrorMessages = new List<string> { "Invalid trend range supplied" };
                        return response;
                }

                var currentQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x =>
                        !x.IsDeleted &&
                        !x.IsIdle &&
                        x.Created >= startDate &&
                        x.Created < endDate);

                var previousQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x =>
                        !x.IsDeleted &&
                        !x.IsIdle &&
                        x.Created >= previousStartDate &&
                        x.Created < previousEndDate);

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    currentQuery = currentQuery.Where(x => x.UserId == userId);
                    previousQuery = previousQuery.Where(x => x.UserId == userId);
                }

                var averageSessionSeconds = await currentQuery
                    .AverageAsync(x => (double?)x.DurationSeconds) ?? 0;

                var peakUsageData = await currentQuery
                    .GroupBy(x => x.Created.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        TotalSeconds = g.Sum(x => x.DurationSeconds)
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .FirstOrDefaultAsync();

                var currentTotalSeconds = await currentQuery
                    .SumAsync(x => (int?)x.DurationSeconds) ?? 0;

                var previousTotalSeconds = await previousQuery
                    .SumAsync(x => (int?)x.DurationSeconds) ?? 0;

                var peakUsageTime = peakUsageData == null
                    ? "N/A"
                    : _helperServ.FormatHourRange(peakUsageData.Hour);

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = new ActivityInsightMetricsDto
                {
                    AvgSessionDuration = _helperServ.FormatTime((int)Math.Round(averageSessionSeconds)),
                    PeakUsageTime = peakUsageTime,
                    WeeklyGrowth = _helperServ.CalculateGrowthPercentage(currentTotalSeconds, previousTotalSeconds)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity insight metrics");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving activity insight metrics" };

                return response;
            }
        }
        public async Task<byte[]> ExportComputerActivities(
            string? userId = null,
            string? appName = null,
            string? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            TrendRange? range = null)
        {
            var query = _userActivityRepo
                .GetQueryable()
                .Where(x => !x.IsDeleted)
                .Include(x => x.User)
                .Include(x => x.MapCategory)
                    .ThenInclude(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(x => x.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(appName))
            {
                var normalizedAppName = appName.Trim().ToLower();
                query = query.Where(x => x.AppName.ToLower().Contains(normalizedAppName));
            }

            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                query = query.Where(x => x.MapCategory.CategoryId == categoryId);
            }

            if (range.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                DateTime startDate;
                DateTime endDate;

                switch (range.Value)
                {
                    case TrendRange.Last7Days:
                        startDate = today.AddDays(-6);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.Last30Days:
                        startDate = today.AddDays(-29);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.CurrentYear:
                        startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = startDate.AddYears(1);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(range), "Invalid range");
                }

                query = query.Where(x => x.Created >= startDate && x.Created < endDate);
            }
            else
            {
                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;
                    query = query.Where(x => x.Created >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.Created < endDate);
                }
            }

            var activities = await query
                .OrderByDescending(x => x.Created)
                .Select(x => new
                {
                    x.UserId,
                    UserName = x.User != null ? x.User.UserName : "N/A",
                    x.AppName,
                    Category = x.MapCategory != null ? x.MapCategory.Category.Name : "N/A",
                    x.WindowTitle,
                    x.EventType,
                    x.StartTime,
                    x.EndTime,
                    x.DurationSeconds,
                    x.IsIdle,
                    x.Created
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Computer Activities");

            worksheet.Cell(1, 1).Value = "User Id";
            worksheet.Cell(1, 2).Value = "Username";
            worksheet.Cell(1, 3).Value = "App Name";
            worksheet.Cell(1, 4).Value = "Category";
            worksheet.Cell(1, 5).Value = "Window Title";
            worksheet.Cell(1, 6).Value = "Event Type";
            worksheet.Cell(1, 7).Value = "Start Time (UTC)";
            worksheet.Cell(1, 8).Value = "End Time (UTC)";
            worksheet.Cell(1, 9).Value = "Duration (Seconds)";
            worksheet.Cell(1, 10).Value = "Formatted Duration";
            worksheet.Cell(1, 11).Value = "Is Idle";
            worksheet.Cell(1, 12).Value = "Created (UTC)";

            var row = 2;

            foreach (var activity in activities)
            {
                worksheet.Cell(row, 1).Value = activity.UserId;
                worksheet.Cell(row, 2).Value = activity.UserName;
                worksheet.Cell(row, 3).Value = activity.AppName;
                worksheet.Cell(row, 4).Value = activity.Category;
                worksheet.Cell(row, 5).Value = activity.WindowTitle;
                worksheet.Cell(row, 6).Value = activity.EventType;
                worksheet.Cell(row, 7).Value = activity.StartTime;
                worksheet.Cell(row, 8).Value = activity.EndTime;
                worksheet.Cell(row, 9).Value = activity.DurationSeconds;
                worksheet.Cell(row, 10).Value = _helperServ.FormatTime(activity.DurationSeconds);
                worksheet.Cell(row, 11).Value = activity.IsIdle ? "Yes" : "No";
                worksheet.Cell(row, 12).Value = activity.Created;

                row++;
            }

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            if (row > 2)
            {
                var tableRange = worksheet.Range($"A1:L{row - 1}");
                tableRange.CreateTable();
            }

            worksheet.Column(7).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";
            worksheet.Column(8).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";
            worksheet.Column(12).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
        public async Task<ResponseDto<ComputerDeviceMetricsDto>> GetComputerDeviceMetrics(TrendRange range)
        {
            var response = new ResponseDto<ComputerDeviceMetricsDto>();

            try
            {
                var today = DateTime.UtcNow.Date;
                DateTime startDate;
                DateTime endDate;

                switch (range)
                {
                    case TrendRange.Last7Days:
                        startDate = today.AddDays(-6);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.Last30Days:
                        startDate = today.AddDays(-29);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.CurrentYear:
                        startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = startDate.AddYears(1);
                        break;

                    default:
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        response.DisplayMessage = "Error";
                        response.ErrorMessages = new List<string> { "Invalid trend range supplied" };
                        return response;
                }

                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x =>
                        !x.IsDeleted &&
                        x.Created >= startDate &&
                        x.Created < endDate);

                var totalComputerDevices = await query
                    .Select(x => x.MachineId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .CountAsync();

                var totalActiveDevices = await query
                    .Where(x => !x.IsIdle)
                    .Select(x => x.MachineId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .CountAsync();

                var totalUsageSeconds = await query
                    .Where(x => !x.IsIdle)
                    .SumAsync(x => (int?)x.DurationSeconds) ?? 0;

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = new ComputerDeviceMetricsDto
                {
                    TotalComputerDevices = totalComputerDevices,
                    TotalActiveDevices = totalActiveDevices,
                    TotalHoursOfUsage = _helperServ.FormatTime(totalUsageSeconds)
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving computer device metrics");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving computer device metrics" };

                return response;
            }
        }
        public async Task<ResponseDto<PaginatedResponseDto<ComputerDeviceListDto>>> PaginateComputerDevices(
            int pageNumber,
            int perPageSize,
            TrendRange? range = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var response = new ResponseDto<PaginatedResponseDto<ComputerDeviceListDto>>();

            try
            {
                if (pageNumber <= 0 || perPageSize <= 0)
                {
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Page number and page size must be greater than zero" };
                    return response;
                }

                var query = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var normalizedSearch = search.Trim().ToLower();
                    query = query.Where(x => x.MachineId.ToLower().Contains(normalizedSearch));
                }

                if (range.HasValue)
                {
                    var today = DateTime.UtcNow.Date;
                    DateTime startDate;
                    DateTime endDate;

                    switch (range.Value)
                    {
                        case TrendRange.Last7Days:
                            startDate = today.AddDays(-6);
                            endDate = today.AddDays(1);
                            break;

                        case TrendRange.Last30Days:
                            startDate = today.AddDays(-29);
                            endDate = today.AddDays(1);
                            break;

                        case TrendRange.CurrentYear:
                            startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            endDate = startDate.AddYears(1);
                            break;

                        default:
                            response.StatusCode = StatusCodes.Status400BadRequest;
                            response.DisplayMessage = "Error";
                            response.ErrorMessages = new List<string> { "Invalid trend range supplied" };
                            return response;
                    }

                    query = query.Where(x => x.Created >= startDate && x.Created < endDate);
                }
                else
                {
                    if (fromDate.HasValue)
                    {
                        var startDate = fromDate.Value.Date;
                        query = query.Where(x => x.Created >= startDate);
                    }

                    if (toDate.HasValue)
                    {
                        var endDate = toDate.Value.Date.AddDays(1);
                        query = query.Where(x => x.Created < endDate);
                    }
                }

                var grouped = await query
                    .Where(x => !string.IsNullOrWhiteSpace(x.MachineId))
                    .GroupBy(x => x.MachineId)
                    .Select(g => new
                    {
                        ComputerName = g.Key,
                        TotalSeconds = g.Where(x => !x.IsIdle).Sum(x => x.DurationSeconds),
                        TotalUserCount = g.Select(x => x.UserId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .ToListAsync();

                var totalCount = grouped.Count;
                var totalUsageSeconds = grouped.Sum(x => x.TotalSeconds);

                var pagedData = grouped
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select((x, index) => new ComputerDeviceListDto
                    {
                        Rank = ((pageNumber - 1) * perPageSize) + index + 1,
                        ComputerName = x.ComputerName,
                        HoursSpent = _helperServ.FormatTime(x.TotalSeconds),
                        TotalUserCount = x.TotalUserCount,
                        PercentageUsage = totalUsageSeconds > 0
                            ? Math.Round((double)x.TotalSeconds / totalUsageSeconds * 100, 1)
                            : 0
                    })
                    .ToList();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = new PaginatedResponseDto<ComputerDeviceListDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    TotalCount = totalCount,
                    Data = pagedData
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving computer devices");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving computer devices" };

                return response;
            }
        }
        public async Task<ResponseDto<List<ComputerDeviceUserUsageDto>>> GetComputerDeviceUserUsage(
            string machineId,
            TrendRange? range = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var response = new ResponseDto<List<ComputerDeviceUserUsageDto>>();

            try
            {
                if (string.IsNullOrWhiteSpace(machineId))
                {
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.DisplayMessage = "Error";
                    response.ErrorMessages = new List<string> { "Machine id is required" };
                    return response;
                }

                var query = _userActivityRepo
                    .GetQueryable()
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && x.MachineId == machineId)
                    .AsQueryable();

                if (range.HasValue)
                {
                    var today = DateTime.UtcNow.Date;
                    DateTime startDate;
                    DateTime endDate;

                    switch (range.Value)
                    {
                        case TrendRange.Last7Days:
                            startDate = today.AddDays(-6);
                            endDate = today.AddDays(1);
                            break;

                        case TrendRange.Last30Days:
                            startDate = today.AddDays(-29);
                            endDate = today.AddDays(1);
                            break;

                        case TrendRange.CurrentYear:
                            startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            endDate = startDate.AddYears(1);
                            break;

                        default:
                            response.StatusCode = StatusCodes.Status400BadRequest;
                            response.DisplayMessage = "Error";
                            response.ErrorMessages = new List<string> { "Invalid trend range supplied" };
                            return response;
                    }

                    query = query.Where(x => x.Created >= startDate && x.Created < endDate);
                }
                else
                {
                    if (fromDate.HasValue)
                    {
                        var startDate = fromDate.Value.Date;
                        query = query.Where(x => x.Created >= startDate);
                    }

                    if (toDate.HasValue)
                    {
                        var endDate = toDate.Value.Date.AddDays(1);
                        query = query.Where(x => x.Created < endDate);
                    }
                }

                var data = await query
                    .GroupBy(x => new
                    {
                        x.UserId,
                        x.User.UserName,
                        x.User.FirstName,
                        x.User.LastName
                    })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.UserName,
                        FullName = g.Key.FirstName + " " + g.Key.LastName,
                        TotalSeconds = g.Where(x => !x.IsIdle).Sum(x => x.DurationSeconds)
                    })
                    .OrderByDescending(x => x.TotalSeconds)
                    .ToListAsync();

                var totalUsageSeconds = data.Sum(x => x.TotalSeconds);

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = data
                    .Select((x, index) => new ComputerDeviceUserUsageDto
                    {
                        Rank = index + 1,
                        UserId = x.UserId,
                        UserName = x.UserName,
                        FullName = x.FullName,
                        HoursSpent = _helperServ.FormatTime(x.TotalSeconds),
                        PercentageUsage = totalUsageSeconds > 0
                            ? Math.Round((double)x.TotalSeconds / totalUsageSeconds * 100, 1)
                            : 0
                    })
                    .ToList();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving computer device user usage");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving computer device user usage" };

                return response;
            }
        }
        public async Task<byte[]> ExportComputerDeviceReport(
            TrendRange? range = null,
            string? search = null,
            string? machineId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _userActivityRepo
                .GetQueryable()
                .Include(x => x.User)
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(x => x.MachineId.ToLower().Contains(normalizedSearch));
            }

            if (!string.IsNullOrWhiteSpace(machineId))
            {
                query = query.Where(x => x.MachineId == machineId);
            }

            if (range.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                DateTime startDate;
                DateTime endDate;

                switch (range.Value)
                {
                    case TrendRange.Last7Days:
                        startDate = today.AddDays(-6);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.Last30Days:
                        startDate = today.AddDays(-29);
                        endDate = today.AddDays(1);
                        break;

                    case TrendRange.CurrentYear:
                        startDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        endDate = startDate.AddYears(1);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(range), "Invalid range");
                }

                query = query.Where(x => x.Created >= startDate && x.Created < endDate);
            }
            else
            {
                if (fromDate.HasValue)
                {
                    var startDate = fromDate.Value.Date;
                    query = query.Where(x => x.Created >= startDate);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.Created < endDate);
                }
            }

            var deviceSummary = await query
                .Where(x => !string.IsNullOrWhiteSpace(x.MachineId))
                .GroupBy(x => x.MachineId)
                .Select(g => new
                {
                    ComputerName = g.Key,
                    TotalSeconds = g.Where(x => !x.IsIdle).Sum(x => x.DurationSeconds),
                    TotalUserCount = g.Select(x => x.UserId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalSeconds)
                .ToListAsync();

            var userUsage = await query
                .Where(x => !string.IsNullOrWhiteSpace(x.MachineId))
                .GroupBy(x => new
                {
                    x.MachineId,
                    x.UserId,
                    x.User.UserName,
                    x.User.FirstName,
                    x.User.LastName
                })
                .Select(g => new
                {
                    g.Key.MachineId,
                    g.Key.UserId,
                    g.Key.UserName,
                    FullName = g.Key.FirstName + " " + g.Key.LastName,
                    TotalSeconds = g.Where(x => !x.IsIdle).Sum(x => x.DurationSeconds)
                })
                .OrderBy(x => x.MachineId)
                .ThenByDescending(x => x.TotalSeconds)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Computer Devices");
            summarySheet.Cell(1, 1).Value = "Rank";
            summarySheet.Cell(1, 2).Value = "Computer Name";
            summarySheet.Cell(1, 3).Value = "Hours Spent";
            summarySheet.Cell(1, 4).Value = "Total User Count";
            summarySheet.Cell(1, 5).Value = "Percentage Usage";

            var totalDeviceUsageSeconds = deviceSummary.Sum(x => x.TotalSeconds);

            var summaryRow = 2;
            foreach (var item in deviceSummary.Select((value, index) => new { value, index }))
            {
                var percentageUsage = totalDeviceUsageSeconds > 0
                    ? Math.Round((double)item.value.TotalSeconds / totalDeviceUsageSeconds * 100, 1)
                    : 0;

                summarySheet.Cell(summaryRow, 1).Value = item.index + 1;
                summarySheet.Cell(summaryRow, 2).Value = item.value.ComputerName;
                summarySheet.Cell(summaryRow, 3).Value = _helperServ.FormatTime(item.value.TotalSeconds);
                summarySheet.Cell(summaryRow, 4).Value = item.value.TotalUserCount;
                summarySheet.Cell(summaryRow, 5).Value = percentageUsage;
                summaryRow++;
            }

            summarySheet.Row(1).Style.Font.Bold = true;
            summarySheet.Columns().AdjustToContents();
            summarySheet.SheetView.FreezeRows(1);

            if (summaryRow > 2)
            {
                summarySheet.Range($"A1:E{summaryRow - 1}").CreateTable();
            }

            var usageSheet = workbook.Worksheets.Add("Device User Usage");
            usageSheet.Cell(1, 1).Value = "Rank";
            usageSheet.Cell(1, 2).Value = "Computer Name";
            usageSheet.Cell(1, 3).Value = "User Id";
            usageSheet.Cell(1, 4).Value = "Username";
            usageSheet.Cell(1, 5).Value = "Full Name";
            usageSheet.Cell(1, 6).Value = "Hours Spent";
            usageSheet.Cell(1, 7).Value = "Percentage Usage";

            var usageRow = 2;
            foreach (var machineGroup in userUsage.GroupBy(x => x.MachineId))
            {
                var machineTotalSeconds = machineGroup.Sum(x => x.TotalSeconds);

                foreach (var item in machineGroup.Select((value, index) => new { value, index }))
                {
                    var percentageUsage = machineTotalSeconds > 0
                        ? Math.Round((double)item.value.TotalSeconds / machineTotalSeconds * 100, 1)
                        : 0;

                    usageSheet.Cell(usageRow, 1).Value = item.index + 1;
                    usageSheet.Cell(usageRow, 2).Value = item.value.MachineId;
                    usageSheet.Cell(usageRow, 3).Value = item.value.UserId;
                    usageSheet.Cell(usageRow, 4).Value = item.value.UserName;
                    usageSheet.Cell(usageRow, 5).Value = item.value.FullName;
                    usageSheet.Cell(usageRow, 6).Value = _helperServ.FormatTime(item.value.TotalSeconds);
                    usageSheet.Cell(usageRow, 7).Value = percentageUsage;
                    usageRow++;
                }
            }

            usageSheet.Row(1).Style.Font.Bold = true;
            usageSheet.Columns().AdjustToContents();
            usageSheet.SheetView.FreezeRows(1);

            if (usageRow > 2)
            {
                usageSheet.Range($"A1:G{usageRow - 1}").CreateTable();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
       

    }
}
