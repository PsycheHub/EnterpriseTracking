using ClosedXML.Excel;
using EnterpriseTracking.Core.Dto.Request.Mailing;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Voucher;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class VoucherService : IVoucherService
    {
        private readonly IEmailServiceViaGmail _emailServices;
        private readonly IHelperServ _helperServ;
        private readonly ILogger<VoucherService> _logger;
        private readonly IEnterpriseTrackingGenericRepo<Voucher> _voucherRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly ITenantContext _tenantContext;
        public VoucherService(IHelperServ helperServ,
            ILogger<VoucherService> logger,
            IEnterpriseTrackingGenericRepo<Voucher> voucherRepo,
            IAccountRepo accountRepo,
            IEmailServiceViaGmail emailServices,
            ITenantContext tenantContext)
        {
            _helperServ = helperServ;
            _logger = logger;
            _voucherRepo = voucherRepo;
            _accountRepo = accountRepo;
            _emailServices = emailServices;
            _tenantContext = tenantContext;
        }

        public async Task<ResponseDto<string>> CreateVoucher(int validatyDays)
        {
            var response = new ResponseDto<string>();
            try
            {
                var now = DateTime.UtcNow;

                var expiryDate = now.AddDays(validatyDays);
                await _voucherRepo.Add(new Voucher()
                {
                    CompanyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("A company account is required"),
                    Code = "VCH-" + _helperServ.GenerateRandomString(8),
                    ExpiredDate = expiryDate,
                });
                await _voucherRepo.SaveChanges();
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully created voucher";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in creating voucher" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<string>> ExtendVoucherLifcycle(string voucherId, int validatyDays)
        {
            var response = new ResponseDto<string>();
            try
            {
                var voucher = await _voucherRepo.GetByIdAsync(voucherId);
                if (voucher == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid voucher id" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var expiryDate = voucher.ExpiredDate.AddDays(validatyDays);

                voucher.ExpiredDate = expiryDate;
                _voucherRepo.Update(voucher);
                await _voucherRepo.SaveChanges();
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully update voucher lifecycle";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in updating voucher lifecycle" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<string>> DeleteVoucher(string voucherId)
        {
            var response = new ResponseDto<string>();
            try
            {
                var voucher = await _voucherRepo.GetByIdAsync(voucherId);
                if (voucher == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid voucher id" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }

                if (voucher.IsLinked)
                {
                    response.ErrorMessages = new List<string>() { "Linked voucher cannot be deleted" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }

                voucher.IsDeleted = true;
                _voucherRepo.Update(voucher);
                await _voucherRepo.SaveChanges();
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully delete voucher";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in deleting voucher" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<string>> LinkVoucher(string voucherId, string userId)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByIdAsync(userId);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the id provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var voucherUserCheck = await _voucherRepo.GetQueryable().FirstOrDefaultAsync(u => u.UserId == userId);
                if (voucherUserCheck != null)
                {
                    response.ErrorMessages = new List<string>() { $"User already linked to an existing voucher no - {voucherUserCheck.Code} " };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var voucher = await _voucherRepo.GetByIdAsync(voucherId);
                if (voucher == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid voucher id" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (voucher.IsLinked ||
                    voucher.UserId != null)
                {
                    response.ErrorMessages = new List<string>() { "Voucher already linked" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }
                voucher.IsLinked = true;
                voucher.UserId = userId;
                voucher.LinkedDate = DateTime.UtcNow;
                voucher.Status = VoucherStatus.Linked.ToString();

                voucher.DateUpdated = DateTime.UtcNow;

                _voucherRepo.Update(voucher);
                await _voucherRepo.SaveChanges();
                findUser.IsVoucherLinked = true;
                await _accountRepo.UpdateUserInfo(findUser);
                var body = $@"
                                <!DOCTYPE html>
                                       <html>
                                        <head>
                                        <meta charset=""UTF-8"" />
                                        <title>Gomtech Voucher</title>
                                        <style>
                                            body {{
                                              font-family: Arial, sans-serif;
                                              background-color: #f9f9f9;
                                              color: #333;
                                              padding: 20px;
                                            }}
                                            .container {{
                                              background-color: #fff;
                                              border-radius: 8px;
                                              padding: 20px;
                                              max-width: 600px;
                                              margin: 0 auto;
                                              box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                                            }}
                                            .btn {{
                                              display: inline-block;
                                              background-color: #007BFF;
                                              color: #fff !important;
                                              padding: 10px 20px;
                                              margin-top: 20px;
                                              border-radius: 5px;
                                              text-decoration: none;
                                              font-weight: bold;
                                            }}
                                            .btn:hover {{
                                              background-color: #0056b3;
                                            }}
                                            p {{
                                              line-height: 1.5;
                                            }}
                                          </style>
                                        </head>
                                        <body>
                                          <div class=""container"">
                                            <h2>Linked Voucher</h2>
                                            <p>Hello {findUser.UserName},</p>
                                            <p>A voucher have been linked to your account</p>
                                            <p>
                                             Voucher :: {voucher.Code}


                                            </p>
                                            
                                            <p>Best regards,<br/>The Gomtech</p>
                                          </div>
                                        </body>
                                        </html>";

                var message = new Message(
                    new[] { findUser.Email },
                    "Linked Voucher",
                    body
                );

                _emailServices.SendEmail(message);


                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully linked vouchers to user";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in linking voucher" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<string>> UnLinkVoucher(string voucherId)
        {
            var response = new ResponseDto<string>();
            try
            {
                var voucher = await _voucherRepo.GetByIdAsync(voucherId);
                if (voucher == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid voucher id" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (!voucher.IsLinked ||
                    voucher.UserId == null)
                {
                    response.ErrorMessages = new List<string>() { "Voucher not yet linked" };
                    response.StatusCode = 500;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var findUser = await _accountRepo.FindUserByIdAsync(voucher.UserId);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the id provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                voucher.IsLinked = false;
                voucher.UserId = null;
                voucher.Status = VoucherStatus.Unlinked.ToString();
                voucher.LinkedDate = null;
                voucher.DateUpdated = DateTime.UtcNow;


                _voucherRepo.Update(voucher);
                findUser.IsVoucherLinked = false;
                await _accountRepo.UpdateUserInfo(findUser);
                await _voucherRepo.SaveChanges();
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully unlinked vouchers to user";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in unlinking voucher" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<PaginatedResponseDto<VoucherRespDto>>> PaginateVoucher(
     int pageNumber,
     int perPageSize,
     string? voucherCodeOrUsername,
     string? status)
        {
            var response = new ResponseDto<PaginatedResponseDto<VoucherRespDto>>();

            try
            {
                var query = _voucherRepo.GetQueryable()
                    .Where(v => !v.IsDeleted)
                    .Include(v => v.User)
                    .AsQueryable();

                // Filter by voucher code or username
                if (!string.IsNullOrWhiteSpace(voucherCodeOrUsername))
                {
                    voucherCodeOrUsername = voucherCodeOrUsername.ToLower();

                    query = query.Where(v =>
                        v.Code.ToLower().Contains(voucherCodeOrUsername) ||
                        (v.User != null && v.User.UserName.ToLower().Contains(voucherCodeOrUsername)));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(v => v.Status == status);
                }

                var totalCount = await query.CountAsync();

                var vouchers = await query
                    .OrderByDescending(v => v.Created)
                    .Skip((pageNumber - 1) * perPageSize)
                    .Take(perPageSize)
                    .Select(v => new
                    {
                        v.Id,
                        v.Code,
                        UserName = v.User != null ? v.User.UserName : "N/A",
                        v.Status,
                        v.Created,
                        v.ExpiredDate
                    })
                    .ToListAsync();

                var voucherDtos = vouchers.Select(v =>
                {
                    int percentage = 0;

                    var totalDays = (v.ExpiredDate - v.Created).TotalDays;

                    if (totalDays > 0)
                    {
                        var remainingDays = (v.ExpiredDate - DateTime.UtcNow).TotalDays;

                        percentage = (int)((remainingDays / totalDays) * 100);

                        percentage = Math.Clamp(percentage, 0, 100);
                    }

                    return new VoucherRespDto
                    {
                        Id = v.Id,
                        Code = v.Code,
                        LinkedTo = v.UserName,
                        Status = v.Status,
                        Created = v.Created,
                        Expired = v.ExpiredDate,
                        ValidatyPercentage = $"{percentage}%"
                    };
                }).ToList();

                var result = new PaginatedResponseDto<VoucherRespDto>
                {
                    CurrentPage = pageNumber,
                    PageSize = perPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / perPageSize),
                    Data = voucherDtos,
                    TotalCount = totalCount
                };

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vouchers");

                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Error retrieving vouchers" };

                return response;
            }
        }

        public async Task<ResponseDto<VoucherMetricsDto>> GetVoucherMetrics()
        {
            var response = new ResponseDto<VoucherMetricsDto>();

            try
            {
                var query = _voucherRepo
                    .GetQueryable()
                    .Where(v => !v.IsDeleted);

                var now = DateTime.UtcNow;
                var expiringSoonDate = now.AddDays(7);

                var totalVouchers = await query.CountAsync();

                var activeVouchers = await query
                    .Where(v => v.ExpiredDate > now && v.Status == "Active")
                    .CountAsync();

                var expiringSoonVouchers = await query
                    .Where(v => v.ExpiredDate > now && v.ExpiredDate <= expiringSoonDate)
                    .CountAsync();

                var expiredVouchers = await query
                    .Where(v => v.ExpiredDate <= now)
                    .CountAsync();

                var metrics = new VoucherMetricsDto
                {
                    TotalVouchers = totalVouchers,
                    ActiveVouchers = activeVouchers,
                    ExpiringSoonVouchers = expiringSoonVouchers,
                    ExpiredVouchers = expiredVouchers
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
                response.ErrorMessages = new List<string> { "Error retrieving voucher metrics" };

                return response;
            }
        }

        public async Task<byte[]> ExportVouchersToExcel(
            DateTime? fromDate,
            DateTime? toDate,
            string? status)
        {
            var query = _voucherRepo
                .GetQueryable()
                .Include(v => v.User)
                .Where(v => !v.IsDeleted);

            // Filter by From Date
            if (fromDate.HasValue)
            {
                query = query.Where(v => v.Created >= fromDate.Value);
            }

            // Filter by To Date
            if (toDate.HasValue)
            {
                query = query.Where(v => v.Created <= toDate.Value);
            }

            // Filter by Status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var vouchers = await query
                .OrderByDescending(v => v.Created)
                .Select(v => new VoucherRespDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    LinkedTo = v.User != null ? v.User.UserName : "Not Linked",
                    Status = v.Status,
                    ValidatyPercentage =
                        v.ExpiredDate <= DateTime.UtcNow
                        ? "0%"
                        : $"{Math.Round((v.ExpiredDate - DateTime.UtcNow).TotalDays / (v.ExpiredDate - v.Created).TotalDays * 100, 2)}%",
                    Created = v.Created,
                    Expired = v.ExpiredDate
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Vouchers");

            // Header Row
            worksheet.Cell(1, 1).Value = "Voucher ID";
            worksheet.Cell(1, 2).Value = "Voucher Code";
            worksheet.Cell(1, 3).Value = "Linked To";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Validity %";
            worksheet.Cell(1, 6).Value = "Created Date";
            worksheet.Cell(1, 7).Value = "Expiry Date";

            int row = 2;

            foreach (var voucher in vouchers)
            {
                worksheet.Cell(row, 1).Value = voucher.Id;
                worksheet.Cell(row, 2).Value = voucher.Code;
                worksheet.Cell(row, 3).Value = voucher.LinkedTo;
                worksheet.Cell(row, 4).Value = voucher.Status;
                worksheet.Cell(row, 5).Value = voucher.ValidatyPercentage;
                worksheet.Cell(row, 6).Value = voucher.Created;
                worksheet.Cell(row, 7).Value = voucher.Expired;

                row++;
            }

            // Style Header
            worksheet.Row(1).Style.Font.Bold = true;

            // Adjust Column Width
            worksheet.Columns().AdjustToContents();

            // Add Table Formatting
            var tableRange = worksheet.Range($"A1:G{row - 1}");
            tableRange.CreateTable();

            // Freeze Header Row
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }


    }
}
