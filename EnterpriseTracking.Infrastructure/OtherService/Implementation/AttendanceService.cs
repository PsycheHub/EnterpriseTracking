using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Attendance;
using EnterpriseTracking.Core.Dto.Response.ComputerActivity;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IEnterpriseTrackingGenericRepo<UserActivity> _userActivityRepo;
        private readonly IEnterpriseTrackingGenericRepo<Attendance> _attendanceRepo;
        private readonly IHelperServ _helperServ;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(IHelperServ helperServ, ILogger<AttendanceService> logger,
            IEnterpriseTrackingGenericRepo<Attendance> attendanceRepo,
            IEnterpriseTrackingGenericRepo<UserActivity> userActivityRepo)
        {
            _helperServ = helperServ;
            _logger = logger;
            _attendanceRepo = attendanceRepo;
            _userActivityRepo = userActivityRepo;
        }

        public async Task<ResponseDto<AttendanceReponseDto>> GetAttendanceMetrics(
   string? userId = null,
   DateTime? fromDate = null,
   DateTime? toDate = null)
        {
            var response = new ResponseDto<AttendanceReponseDto>();

            try
            {
                // =========================
                // 🔹 BASE QUERY (ACTIVITY)
                // =========================
                var activityQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted);

                var attendanceQuery = _attendanceRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && x.FirstLoginUtc.HasValue);

                // =========================
                // 🔹 OPTIONAL FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    activityQuery = activityQuery.Where(x => x.UserId == userId);
                    attendanceQuery = attendanceQuery.Where(x => x.UserId == userId);
                }

                // =========================
                // 🔹 OPTIONAL FILTER: DATE RANGE
                // =========================
                if (fromDate.HasValue)
                {
                    var from = fromDate.Value.Date;
                    activityQuery = activityQuery.Where(x => x.StartTime >= from);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc >= from);
                }

                if (toDate.HasValue)
                {
                    var to = toDate.Value.Date.AddDays(1);
                    activityQuery = activityQuery.Where(x => x.StartTime < to);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc < to);
                }

                // =========================
                // 🔹 GROUP ACTIVITIES BY USER + DAY (GET LAST EVENT)
                // =========================
                var lastActivitiesPerDay = await activityQuery
                    .Where(x => !x.IsIdle) // optional: exclude idle
                    .GroupBy(x => new { x.UserId, Date = x.StartTime.Date })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.Date,
                        LastEndTime = g.Max(x => x.EndTime)
                    }).ToListAsync();

                // =========================
                // 🔹 JOIN WITH ATTENDANCE (FIRST LOGIN)
                // =========================
                var attendanceSessions = await attendanceQuery
                    .Select(a => new
                    {
                        a.UserId,
                        Date = a.FirstLoginUtc.Value.Date,
                        FirstLogin = a.FirstLoginUtc.Value
                    })
                    .ToListAsync();

                // =========================
                // 🔹 BUILD DAILY SESSIONS
                // =========================
                var sessions = (from att in attendanceSessions
                                join act in lastActivitiesPerDay
                                on new { att.UserId, att.Date }
                                equals new { act.UserId, act.Date }
                                select new
                                {
                                    att.UserId,
                                    att.Date,
                                    DurationSeconds = (int)(act.LastEndTime - att.FirstLogin).TotalSeconds
                                })
                                .Where(x => x.DurationSeconds > 0)
                                .ToList();

                // =========================
                // 🔹 TOTAL ATTENDANCE TIME
                // =========================
                var totalSeconds = sessions.Sum(x => x.DurationSeconds);

                // =========================
                // 🔹 DISTINCT USERS
                // =========================
                var totalUsers = sessions
                    .Select(x => x.UserId)
                    .Distinct()
                    .Count();

                // =========================
                // 🔹 AVG TIME PER USER
                // =========================
                var avgPerUserSeconds = totalUsers > 0
                    ? totalSeconds / totalUsers
                    : 0;

                // =========================
                // 🔹 AVG LOGIN DURATION (per session)
                // =========================
                var avgSessionSeconds = sessions.Any()
                    ? (int)sessions.Average(x => x.DurationSeconds)
                    : 0;

                // =========================
                // 🔹 RESPONSE
                // =========================
                var metrics = new AttendanceReponseDto
                {
                    TotalAttendanceTime = _helperServ.FormatTime(totalSeconds),
                    AvgTimePerUser = _helperServ.FormatTime(avgPerUserSeconds),
                    AvgLoginDuration = _helperServ.FormatTime(avgSessionSeconds)
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
                response.ErrorMessages = new List<string> { "Error retrieving attendance metrics" };

                return response;
            }
        }



        public async Task<ResponseDto<PaginatedResponseDto<AttendanceRecordDto>>> PaginateAttendanceRecords(
    int pageNumber,
    int perPageSize,
    string? userId,
    string? search,
    DateTime? startDate,
    DateTime? endDate)
        {
            var response = new ResponseDto<PaginatedResponseDto<AttendanceRecordDto>>();

            try
            {
                // =========================
                // 🔹 BASE QUERIES
                // =========================
                var attendanceQuery = _attendanceRepo
                    .GetQueryable()
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && x.FirstLoginUtc.HasValue);

                var activityQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted);

                // =========================
                // 🔹 FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    attendanceQuery = attendanceQuery.Where(x => x.UserId == userId);
                    activityQuery = activityQuery.Where(x => x.UserId == userId);
                }

                // =========================
                // 🔹 FILTER: SEARCH (USERNAME)
                // =========================
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    attendanceQuery = attendanceQuery.Where(x =>
                        (x.User.FirstName + " " + x.User.LastName).ToLower().Contains(s)
                        || x.User.UserName.ToLower().Contains(s));
                }

                // =========================
                // 🔹 FILTER: DATE RANGE
                // =========================
                if (startDate.HasValue)
                {
                    var from = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc >= from);
                    activityQuery = activityQuery.Where(x => x.StartTime >= from);
                }

                if (endDate.HasValue)
                {
                    var to = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc < to);
                    activityQuery = activityQuery.Where(x => x.StartTime < to);
                }

                // =========================
                // 🔹 MATERIALIZE (IMPORTANT)
                // =========================
                var attendances = await attendanceQuery
                    .Select(a => new
                    {
                        a.UserId,
                        UserName = a.User.FirstName + " " + a.User.LastName,
                        Date = a.FirstLoginUtc.Value.Date,
                        FirstLogin = a.FirstLoginUtc.Value
                    })
                    .ToListAsync();

                var activities = await activityQuery
                    .Select(a => new
                    {
                        a.UserId,
                        Date = a.StartTime.Date,
                        a.EndTime,
                        a.DurationSeconds,
                        a.IsIdle
                    })
                    .ToListAsync();

                // =========================
                // 🔹 GROUP ACTIVITIES
                // =========================
                var activityGrouped = activities
                    .GroupBy(x => new { x.UserId, x.Date })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.Date,
                        LastActivity = g.Max(x => x.EndTime),
                        IdleSeconds = g.Where(x => x.IsIdle).Sum(x => x.DurationSeconds)
                    })
                    .ToList();

                // =========================
                // 🔹 JOIN + BUILD RESULT
                // =========================
                var resultQuery = (from att in attendances
                                   join act in activityGrouped
                                   on new { att.UserId, att.Date }
                                   equals new { act.UserId, act.Date }
                                   into gj
                                   from act in gj.DefaultIfEmpty()
                                   select new AttendanceRecordDto
                                   {
                                       UserName = att.UserName,
                                       Date = att.Date,
                                       FirstLogin = att.FirstLogin,
                                       LastActivity = act?.LastActivity,

                                       TotalActivityTime = act != null
                                           ? _helperServ.FormatTime((int)(act.LastActivity - att.FirstLogin).TotalSeconds)
                                           : "0s",

                                       IdleTime = act != null
                                           ? _helperServ.FormatTime(act.IdleSeconds)
                                           : "0s"
                                   })
                                   .OrderByDescending(x => x.Date)
                                   .ThenByDescending(x => x.LastActivity)
                                   .AsQueryable();

                // =========================
                // 🔹 PAGINATION
                // =========================
                var totalCount = resultQuery.Count();

                var data = resultQuery
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .ToList();

                var result = new PaginatedResponseDto<AttendanceRecordDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = data,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Attendance records retrieved successfully";
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
            "Error retrieving attendance records"
        };

                return response;
            }
        }


        public async Task<ResponseDto<List<ActivityTrendDto>>> GetAttendanceTrend(
    TrendRange range,
    string? userId = null)
        {
            var response = new ResponseDto<List<ActivityTrendDto>>();

            try
            {
                // =========================
                // 🔹 BASE QUERIES
                // =========================
                var attendanceQuery = _attendanceRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && x.FirstLoginUtc.HasValue);

                var activityQuery = _userActivityRepo
                    .GetQueryable()
                    .Where(x => !x.IsDeleted && !x.IsIdle);

                // =========================
                // 🔹 FILTER: USER
                // =========================
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    attendanceQuery = attendanceQuery.Where(x => x.UserId == userId);
                    activityQuery = activityQuery.Where(x => x.UserId == userId);
                }

                var today = DateTime.UtcNow.Date;

                List<ActivityTrendDto> result;

                // =========================
                // 🔹 PRELOAD DATA (IMPORTANT)
                // =========================
                var attendances = await attendanceQuery
                    .Select(a => new
                    {
                        a.UserId,
                        Date = a.FirstLoginUtc.Value.Date,
                        FirstLogin = a.FirstLoginUtc.Value
                    })
                    .ToListAsync();

                var activities = await activityQuery
                    .Select(a => new
                    {
                        a.UserId,
                        Date = a.StartTime.Date,
                        a.EndTime
                    })
                    .ToListAsync();

                // =========================
                // 🔹 GROUP LAST ACTIVITY PER DAY
                // =========================
                var lastActivities = activities
                    .GroupBy(x => new { x.UserId, x.Date })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.Date,
                        LastEndTime = g.Max(x => x.EndTime)
                    })
                    .ToList();

                // =========================
                // 🔹 BUILD DAILY ATTENDANCE SECONDS
                // =========================
                var dailyAttendance = (from att in attendances
                                       join act in lastActivities
                                       on new { att.UserId, att.Date }
                                       equals new { act.UserId, act.Date }
                                       select new
                                       {
                                           att.Date,
                                           Seconds = (int)(act.LastEndTime - att.FirstLogin).TotalSeconds
                                       })
                                       .Where(x => x.Seconds > 0)
                                       .ToList();

                // =========================
                // 🔹 SWITCH RANGE
                // =========================
                switch (range)
                {
                    // =========================
                    // 🔹 LAST 7 DAYS
                    // =========================
                    case TrendRange.Last7Days:
                        {
                            var startDate = today.AddDays(-6);

                            var data = dailyAttendance
                                .Where(x => x.Date >= startDate)
                                .GroupBy(x => x.Date)
                                .Select(g => new
                                {
                                    Date = g.Key,
                                    TotalSeconds = g.Sum(x => x.Seconds)
                                })
                                .ToList();

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
                            var startDate = today.AddDays(-29);

                            var data = dailyAttendance
                                .Where(x => x.Date >= startDate)
                                .GroupBy(x => x.Date)
                                .Select(g => new
                                {
                                    Date = g.Key,
                                    TotalSeconds = g.Sum(x => x.Seconds)
                                })
                                .ToList();

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
                            var year = today.Year;

                            var data = dailyAttendance
                                .Where(x => x.Date.Year == year)
                                .GroupBy(x => x.Date.Month)
                                .Select(g => new
                                {
                                    Month = g.Key,
                                    TotalSeconds = g.Sum(x => x.Seconds)
                                })
                                .ToList();

                            result = Enumerable.Range(1, 12)
                                .Select(month =>
                                {
                                    var match = data.FirstOrDefault(x => x.Month == month);
                                    var seconds = match?.TotalSeconds ?? 0;

                                    var date = new DateTime(year, month, 1);

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
                response.ErrorMessages = new List<string> { "Error retrieving attendance trend" };

                return response;
            }
        }
        public async Task<byte[]> ExportAttendanceRecords(
            string? userId,
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            TrendRange? range)
        {
            var attendanceQuery = _attendanceRepo
                .GetQueryable()
                .Include(x => x.User)
                .Where(x => !x.IsDeleted && x.FirstLoginUtc.HasValue);

            var activityQuery = _userActivityRepo
                .GetQueryable()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                attendanceQuery = attendanceQuery.Where(x => x.UserId == userId);
                activityQuery = activityQuery.Where(x => x.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                attendanceQuery = attendanceQuery.Where(x =>
                    (x.User.FirstName + " " + x.User.LastName).ToLower().Contains(s) ||
                    x.User.UserName.ToLower().Contains(s));
            }

            if (range.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                DateTime from;
                DateTime to;

                switch (range.Value)
                {
                    case TrendRange.Last7Days:
                        from = DateTime.SpecifyKind(today.AddDays(-6), DateTimeKind.Utc);
                        to = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);
                        break;

                    case TrendRange.Last30Days:
                        from = DateTime.SpecifyKind(today.AddDays(-29), DateTimeKind.Utc);
                        to = DateTime.SpecifyKind(today.AddDays(1), DateTimeKind.Utc);
                        break;

                    case TrendRange.CurrentYear:
                        from = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        to = from.AddYears(1);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(range), "Invalid range");
                }

                attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc >= from && x.FirstLoginUtc < to);
                activityQuery = activityQuery.Where(x => x.StartTime >= from && x.StartTime < to);
            }
            else
            {
                if (startDate.HasValue)
                {
                    var from = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc >= from);
                    activityQuery = activityQuery.Where(x => x.StartTime >= from);
                }

                if (endDate.HasValue)
                {
                    var to = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                    attendanceQuery = attendanceQuery.Where(x => x.FirstLoginUtc < to);
                    activityQuery = activityQuery.Where(x => x.StartTime < to);
                }
            }

            var attendances = await attendanceQuery
                .Select(a => new
                {
                    a.UserId,
                    UserName = a.User.UserName,
                    FullName = a.User.FirstName + " " + a.User.LastName,
                    Date = a.FirstLoginUtc.Value.Date,
                    FirstLogin = a.FirstLoginUtc.Value
                })
                .ToListAsync();

            var activities = await activityQuery
                .Select(a => new
                {
                    a.UserId,
                    Date = a.StartTime.Date,
                    a.EndTime,
                    a.DurationSeconds,
                    a.IsIdle
                })
                .ToListAsync();

            var activityGrouped = activities
                .GroupBy(x => new { x.UserId, x.Date })
                .Select(g => new
                {
                    g.Key.UserId,
                    g.Key.Date,
                    LastActivity = g.Max(x => x.EndTime),
                    IdleSeconds = g.Where(x => x.IsIdle).Sum(x => x.DurationSeconds)
                })
                .ToList();

            var records = (from att in attendances
                           join act in activityGrouped
                           on new { att.UserId, att.Date }
                           equals new { act.UserId, act.Date }
                           into gj
                           from act in gj.DefaultIfEmpty()
                           select new AttendanceRecordDto
                           {
                               UserName = att.UserName,
                               FullName = att.FullName,
                               Date = att.Date,
                               FirstLogin = att.FirstLogin,
                               LastActivity = act?.LastActivity,
                               TotalActivityTime = act != null
                                   ? _helperServ.FormatTime((int)(act.LastActivity - att.FirstLogin).TotalSeconds)
                                   : "0s",
                               IdleTime = act != null
                                   ? _helperServ.FormatTime(act.IdleSeconds)
                                   : "0s"
                           })
                           .OrderByDescending(x => x.Date)
                           .ThenByDescending(x => x.LastActivity)
                           .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Username";
            worksheet.Cell(1, 2).Value = "Full Name";
            worksheet.Cell(1, 3).Value = "Date";
            worksheet.Cell(1, 4).Value = "First Login (UTC)";
            worksheet.Cell(1, 5).Value = "Last Activity (UTC)";
            worksheet.Cell(1, 6).Value = "Total Activity Time";
            worksheet.Cell(1, 7).Value = "Idle Time";

            var row = 2;

            foreach (var item in records)
            {
                worksheet.Cell(row, 1).Value = item.UserName;
                worksheet.Cell(row, 2).Value = item.FullName;
                worksheet.Cell(row, 3).Value = item.Date;
                worksheet.Cell(row, 4).Value = item.FirstLogin;
                worksheet.Cell(row, 5).Value = item.LastActivity;
                worksheet.Cell(row, 6).Value = item.TotalActivityTime;
                worksheet.Cell(row, 7).Value = item.IdleTime;
                row++;
            }

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            worksheet.Column(3).Style.DateFormat.Format = "yyyy-mm-dd";
            worksheet.Column(4).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";
            worksheet.Column(5).Style.DateFormat.Format = "yyyy-mm-dd HH:mm:ss";

            if (row > 2)
            {
                var tableRange = worksheet.Range($"A1:G{row - 1}");
                tableRange.CreateTable();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
