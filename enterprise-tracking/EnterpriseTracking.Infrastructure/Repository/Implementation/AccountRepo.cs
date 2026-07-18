using ClosedXML.Excel;
using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Dto.Response.Auth;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using EnterpriseTracking.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTracking.Infrastructure.Repository.Implementation
{
    public class AccountRepo : IAccountRepo
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EnterpriseTrackingContext _context;
        private readonly IHelperServ _helperServ;

        public AccountRepo(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            EnterpriseTrackingContext context,IHelperServ helperServ)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _helperServ = helperServ;
        }
        public async Task<bool> AddRoleAsync(ApplicationUser user, string Role)
        {
            var AddRole = await _userManager.AddToRoleAsync(user, Role);
            if (AddRole.Succeeded)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> RemoveRoleAsync(ApplicationUser user, IList<string> role)
        {
            var removeRole = await _userManager.RemoveFromRolesAsync(user, role);
            if (removeRole.Succeeded)
            {
                return true;
            }
            return false;
        }

        public async Task<IList<string>> GetUserRoles(ApplicationUser user)
        {
            var getRoles = await _userManager.GetRolesAsync(user);
            if (getRoles != null)
            {
                return getRoles;
            }
            return null;
        }

        public async Task<bool> RoleExist(string Role)
        {
            var check = await _roleManager.RoleExistsAsync(Role);
            return check;
        }
        public async Task<bool> ConfirmEmail(string token, ApplicationUser user)
        {
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return true;
            }
            return false;
        }



        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            var findUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (findUser == null)
            {
                return null;
            }
            return findUser;
        }
        public async Task<ApplicationUser?> FindUserByuSERNAMEAsync(string userName)
        {
            var findUser = await _context.Users.Include(u=>u.Voucher).FirstOrDefaultAsync(u=>u.UserName.ToLower() == userName.ToLower());
            if (findUser == null)
            {
                return null;
            }
            return findUser;
        }

        public async Task<ApplicationUser> FindUserByIdAsync(string id)
        {
            var findUser = await _userManager.FindByIdAsync(id);
            return findUser;
        }
        public async Task<DisplayFindUserDTO> FindUserByIdSingleAsync(string id)
        {
            var findUser = await _context.Users.Where(u => u.Id == id).Select(u => new DisplayFindUserDTO
            {
                Id = u.Id,
                Email = u.Email,
                Created = u.Created,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Status = u.Status,
                IsSuspend = u.IsSuspend,
                UserName = u.UserName,
                IsDeleted = u.IsDeleted,
                LastLoginTime = u.LastLoginTime,
                DeleteDate = u.DeleteDate,

            }).FirstOrDefaultAsync();
            return findUser;
        }
        public async Task<PaginatedUser> GetPaginatedUserInfoAsync(
    int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked)
        {
            var query = _context.Users.AsQueryable();

            // Filter by name or email
            if (!string.IsNullOrWhiteSpace(nameOrEmail))
            {
                query = query.Where(u =>
                    u.Email.Contains(nameOrEmail) ||
                    u.FirstName.Contains(nameOrEmail) ||
                    u.LastName.Contains(nameOrEmail) ||
                    u.UserName.Contains(nameOrEmail));
            }

            // Filter by status
            if (status != null)
            {
                var statusString = status.ToString();
                query = query.Where(u => u.Status == statusString);
            }

            // Filter by voucher linkage
            if (isVoucherLinked != null)
            {
                query = query.Where(u => u.IsVoucherLinked == isVoucherLinked);
            }

            // Exclude deleted users
            query = query.Where(u => !u.IsDeleted);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.Created)
                .Skip((pageNumber - 1) * perPageSize)
                .Take(perPageSize)
                .Select(u => new DisplayFindUserDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Created = u.Created,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Status = u.Status,
                    IsSuspend = u.IsSuspend,
                    UserName = u.UserName,
                    IsDeleted = u.IsDeleted,
                    LastLoginTime = u.LastLoginTime,
                    DeleteDate = u.DeleteDate
                })
                .ToListAsync();

            return new PaginatedUser
            {
                CurrentPage = pageNumber,
                PageSize = perPageSize,
                TotalUserCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)perPageSize),
                Data = users
            };
        }
        public async Task<ApplicationUser> FindUserByIdFullinfoAsync(string id)
        {
            var findUser = await _userManager.Users.FirstOrDefaultAsync(d => d.Id == id);
            return findUser;
        }

        public async Task<string> ForgotPassword(ApplicationUser user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return token;
        }
      

        public async Task<bool> CheckAccountPassword(ApplicationUser user, string password)
        {
            var checkUserPassword = await _userManager.CheckPasswordAsync(user, password);
            return checkUserPassword;
        }

        public async Task<bool> ResetPasswordAsync(ApplicationUser user, ResetPassword resetPassword)
        {
            var result = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
            if (result.Succeeded)
            {
                return true;
            }
            return false;
        }

        public async Task<ApplicationUser> SignUpAsync(ApplicationUser user, string Password)
        {
            var result = await _userManager.CreateAsync(user, Password);
            if (result.Succeeded)
            {
                return user;
            }
            return null;
        }

        public async Task<bool> UpdateUserInfo(ApplicationUser applicationUser)
        {
            var updateUserInfo = await _userManager.UpdateAsync(applicationUser);
            if (updateUserInfo.Succeeded)
            {
                return true;
            }
            return false;
        }



        public async Task<byte[]> ExportUsersAsync(ExportUserRequest request)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            

            // Filter Status
            if (request.Status != null)
            {
                var status = request.Status.ToString();
                query = query.Where(u => u.Status == status);
            }

            // Filter Voucher
            if (request.IsVoucherLinked != null)
            {
                query = query.Where(u => u.IsVoucherLinked == request.IsVoucherLinked);
            }

            // Filter Date Range
            if (request.FromDate != null)
            {
                query = query.Where(u => u.Created >= request.FromDate);
            }

            if (request.ToDate != null)
            {
                query = query.Where(u => u.Created <= request.ToDate);
            }

            query = query.Where(u => !u.IsDeleted);

            var users = await query
                .OrderByDescending(u => u.Created)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.UserName,
                    u.Status,
                    u.IsVoucherLinked,
                    u.IsSuspend,
                    u.Created,
                    u.LastLoginTime
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Users");

            // Header
            worksheet.Cell(1, 1).Value = "User Id";
            worksheet.Cell(1, 2).Value = "First Name";
            worksheet.Cell(1, 3).Value = "Last Name";
            worksheet.Cell(1, 4).Value = "Email";
            worksheet.Cell(1, 5).Value = "Username";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Voucher Linked";
            worksheet.Cell(1, 8).Value = "Suspended";
            worksheet.Cell(1, 9).Value = "Created";
            worksheet.Cell(1, 10).Value = "Last Login";

            var row = 2;

            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value = user.Id;
                worksheet.Cell(row, 2).Value = user.FirstName;
                worksheet.Cell(row, 3).Value = user.LastName;
                worksheet.Cell(row, 4).Value = user.Email;
                worksheet.Cell(row, 5).Value = user.UserName;
                worksheet.Cell(row, 6).Value = user.Status;
                worksheet.Cell(row, 7).Value = user.IsVoucherLinked;
                worksheet.Cell(row, 8).Value = user.IsSuspend;
                worksheet.Cell(row, 9).Value = user.Created;
                worksheet.Cell(row, 10).Value = user.LastLoginTime;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public async Task<UserGrowthMetricsDto> GetUserGrowthMetricsAsync()
        {

            var currentUsers =  await _context.Users
                .AsNoTracking()
                .ToListAsync();

           

            var totalCurrent = currentUsers.Count;
            

            var activeCurrent = currentUsers.Count(x => x.Status == UserStatus.Active.ToString());
          

            var suspendedCurrent = currentUsers.Count(x => x.IsSuspend);
           

            var invitedCurrent = currentUsers.Count(x => x.Status == UserStatus.Invited.ToString());
           

            return new UserGrowthMetricsDto
            {
                TotalUsers = totalCurrent,
               

                ActiveUsers = activeCurrent,
           

                SuspendedUsers = suspendedCurrent,
               

                InvitedUsers = invitedCurrent,
               
            };
        }
    }
}
