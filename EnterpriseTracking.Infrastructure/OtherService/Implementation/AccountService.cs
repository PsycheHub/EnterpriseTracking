using EnterpriseTracking.Core.Dto.Request.Auth;
using EnterpriseTracking.Core.Dto.Request.Mailing;
using EnterpriseTracking.Core.Dto.Response;
using EnterpriseTracking.Core.Dto.Response.Auth;
using EnterpriseTracking.Core.Entities;
using EnterpriseTracking.Core.Enum;
using EnterpriseTracking.Core.OtherService.Interface;
using EnterpriseTracking.Core.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace EnterpriseTracking.Infrastructure.OtherService.Implementation
{
    public class AccountService : IAccountService
    {

        private readonly IAccountRepo _accountRepo;
        private readonly IEmailServiceViaGmail _emailServices;
        private readonly IHelperServ _helperServ;
        private readonly ILogger<AccountService> _logger;
        private readonly IEnterpriseTrackingGenericRepo<Voucher> _voucherRepo;
        private readonly IEnterpriseTrackingGenericRepo<AgentSession> _agentSessionRepo;
        private readonly IGenerateJwt _generateJwt;
        private readonly IEncryptionService _encryption;
        private readonly IConfiguration _configuration;

        public AccountService(
            IAccountRepo accountRepo,
            ILogger<AccountService> logger,
            IGenerateJwt generateJwt,
            IEmailServiceViaGmail emailServices,
         IHelperServ helperServ,
            IEncryptionService encryption,
            IConfiguration configuration,
            IEnterpriseTrackingGenericRepo<Voucher> voucherRepo,
            IEnterpriseTrackingGenericRepo<AgentSession> agentSessionRepo)
        {
            _accountRepo = accountRepo;
            _logger = logger;
            _generateJwt = generateJwt;
            _emailServices = emailServices;
            _helperServ = helperServ;
            _configuration = configuration;
            _encryption = encryption;
            _voucherRepo = voucherRepo;
            _agentSessionRepo = agentSessionRepo;
        }

        public async Task<ResponseDto<string>> RegisterUser(SignUp signUp, string Role)
        {
            var response = new ResponseDto<string>();
            try
            {
                var checkUserExist = await _accountRepo.FindUserByEmailAsync(signUp.Email);
                if (checkUserExist != null)
                {
                    response.ErrorMessages = new List<string>() { "User with the email already exist" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var checkRole = await _accountRepo.RoleExist(Role);
                if (checkRole == false)
                {
                    response.ErrorMessages = new List<string>() { "Role is not available" };
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var mapAccount = new ApplicationUser();

                mapAccount.Email = signUp.Email;
                mapAccount.FirstName = signUp.FirstName;
                mapAccount.LastName = signUp.LastName;
                mapAccount.UserName = _helperServ.UsernameGenerator(signUp.Email);


                var generatePassowrd = _helperServ.GenerateRandomString(8);
                var createUser = await _accountRepo.SignUpAsync(mapAccount, generatePassowrd);
                if (createUser == null)
                {
                    response.ErrorMessages = new List<string>() { "User not created successfully" };
                    response.StatusCode = StatusCodes.Status501NotImplemented;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var addRole = await _accountRepo.AddRoleAsync(createUser, Role);
                if (addRole == false)
                {
                    response.ErrorMessages = new List<string>() { "Fail to add role to user" };
                    response.StatusCode = StatusCodes.Status501NotImplemented;
                    response.DisplayMessage = "Error";
                    return response;
                }





                var body = $@"
                                <!DOCTYPE html>
                                       <html>
                                        <head>
                                        <meta charset=""UTF-8"" />
                                        <title>Gomtech Invitation</title>
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
                                            <h2>Invite user</h2>
                                            <p>Hello {createUser.UserName},</p>
                                            <p>Thank you for registering. Your credential are below:</p>
                                            <p>
                                             Usernamme :: {createUser.UserName},
Password :: {generatePassowrd},

                                            </p>
                                            
                                            <p>Best regards,<br/>The Team</p>
                                          </div>
                                        </body>
                                        </html>";

                var message = new Message(
                    new[] { createUser.Email },
                    "Invite User",
                    body
                );

                _emailServices.SendEmail(message);

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Successful";
                response.Result = "User successfully created";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in resgistering the user" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }

        public async Task<ResponseDto<string>> UpdateUserRole(string id, string role)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByIdAsync(id);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the email provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var checkRole = await _accountRepo.RoleExist(role);
                if (checkRole == false)
                {
                    response.ErrorMessages = new List<string>() { "Role is not available" };
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var getExistingRoles = await _accountRepo.GetUserRoles(findUser);
                if (getExistingRoles.Count > 0)
                {
                    var removeExistingRoles = await _accountRepo.RemoveRoleAsync(findUser, getExistingRoles);
                    if (removeExistingRoles == false)
                    {
                        response.ErrorMessages = new List<string>() { "Error in removing role for user" };
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        response.DisplayMessage = "Error";
                        return response;
                    }
                }

                var addRole = await _accountRepo.AddRoleAsync(findUser, role);
                if (addRole == false)
                {
                    response.ErrorMessages = new List<string>() { "Fail to add role to user" };
                    response.StatusCode = StatusCodes.Status501NotImplemented;
                    response.DisplayMessage = "Error";
                    return response;
                }
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Successful";
                response.Result = "User role updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in updating user role" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }

        public async Task<ResponseDto<LoginResultDto>> LoginUser(SignInModel signIn)
        {
            var response = new ResponseDto<LoginResultDto>();
            try
            {
                var checkUserExist = await _accountRepo.FindUserByuSERNAMEAsync(signIn.Username);
                if (checkUserExist == null)
                {
                    _logger.LogError("user does not exist", JsonConvert.SerializeObject(checkUserExist));
                    response.ErrorMessages = new List<string>() { "There is no user with the username provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (checkUserExist.IsSuspend == true)
                {
                    response.ErrorMessages = new List<string>() { "User is suspended, contact admin" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var checkPassword = await _accountRepo.CheckAccountPassword(checkUserExist, signIn.Password);
                if (checkPassword == false)
                {
                    response.ErrorMessages = new List<string>() { "Invalid Email or Password" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }

                checkUserExist.LastLoginTime = DateTime.UtcNow;
                await _accountRepo.UpdateUserInfo(checkUserExist);

                var generateToken = await _generateJwt.GenerateToken(checkUserExist);
                if (generateToken == null)
                {
                    response.ErrorMessages = new List<string>() { "Error in generating jwt for user" };
                    response.StatusCode = 501;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var getUserRole = await _accountRepo.GetUserRoles(checkUserExist);
                if (!getUserRole.Contains("Admin"))
                {
                    response.ErrorMessages = new List<string>() { "User is not an admin" };
                    response.StatusCode = 501;
                    response.DisplayMessage = "Error";
                    return response;
                }
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Successfully login";
                response.Result = new LoginResultDto() { Jwt = generateToken, UserRole = getUserRole };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in login the user" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }

        public async Task<ResponseDto<LoginResultDto>> LoginAgentUser(AgentSignInUserReq signIn)
        {
            var response = new ResponseDto<LoginResultDto>();
            try
            {
                var checkUserExist = await _accountRepo.FindUserByuSERNAMEAsync(signIn.Username);
                if (checkUserExist == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the username provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (checkUserExist.IsSuspend == true)
                {
                    response.ErrorMessages = new List<string>() { "User is suspended, contact admin" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var checkPassword = await _accountRepo.CheckAccountPassword(checkUserExist, signIn.Password);
                if (checkPassword == false)
                {
                    response.ErrorMessages = new List<string>() { "Invalid username or Password" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }
               /* if (checkUserExist.IsVoucherLinked == false)
                {
                    response.ErrorMessages = new List<string>() { "Account not yet linked, pls contact admin to linked voucher to your account" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (checkUserExist.Voucher.Status == VoucherStatus.Linked.ToString() &&
                    string.IsNullOrEmpty(signIn.Voucher))
                {
                    response.ErrorMessages = new List<string>() { "Account linked, pls provide voucher to activate account" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (checkUserExist.IsVoucherLinked == true &&
                    checkUserExist.Voucher.Code != signIn.Voucher && !string.IsNullOrEmpty(signIn.Voucher))
                {
                    response.ErrorMessages = new List<string>() { "Invalid voucher linked to Account" };
                    response.StatusCode = 400;
                    response.DisplayMessage = "Error";
                    return response;
                }
                if (checkUserExist.Voucher.Status == VoucherStatus.Linked.ToString() && checkUserExist.Voucher.Code == signIn.Voucher)
                {
                    checkUserExist.Voucher.Status = VoucherStatus.Active.ToString();

                    checkUserExist.Voucher.DateUpdated = DateTime.UtcNow;

                    _voucherRepo.Update(checkUserExist.Voucher);
                    await _voucherRepo.SaveChanges();
                }*/
                checkUserExist.Status = UserStatus.Active.ToString();
                checkUserExist.LastLoginTime = DateTime.UtcNow;
                await _accountRepo.UpdateUserInfo(checkUserExist);

                var generateToken = await _generateJwt.GenerateToken(checkUserExist);
                if (generateToken == null)
                {
                    response.ErrorMessages = new List<string>() { "Error in generating jwt for user" };
                    response.StatusCode = 501;
                    response.DisplayMessage = "Error";
                    return response;
                }

                var getUserRole = await _accountRepo.GetUserRoles(checkUserExist);

                var tokenBytes = System.Text.Encoding.UTF8.GetBytes(generateToken);
                var tokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
                var expiresAt = DateTime.UtcNow.AddDays(2);

                var session = new AgentSession
                {
                    UserId = checkUserExist.Id,
                    MachineName = Environment.MachineName,
                    TokenHash = tokenHash,
                    LoginTime = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };
                await _agentSessionRepo.Add(session);
                await _agentSessionRepo.SaveChanges();

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Successfully login";
                response.Result = new LoginResultDto()
                {
                    Jwt = generateToken,
                    UserId = checkUserExist.Id,
                    UserRole = getUserRole
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in login the user via agent login" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }


        public async Task<ResponseDto<UserInfo>> UserInfoAsync(string userId)
        {
            var response = new ResponseDto<UserInfo>();
            try
            {
                var fetchUser = await _accountRepo.FindUserByIdFullinfoAsync(userId);
                if (fetchUser == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid user" };
                    response.DisplayMessage = "Error";
                    response.StatusCode = 400;
                    return response;
                }

                var getUserRole = await _accountRepo.GetUserRoles(fetchUser);





                var result = new UserInfo()
                {
                    Id = fetchUser.Id,
                    Email = fetchUser.Email,
                    UserName = fetchUser.UserName,
                    FirstName = fetchUser.FirstName,
                    LastName = fetchUser.LastName,


                    IsSuspendUser = fetchUser.IsSuspend,


                    LastLoginTime = fetchUser.LastLoginTime,
                    Created = fetchUser.Created,
                    UserRole = getUserRole,


                };


                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = result;
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in getting user info" };
                response.StatusCode = 501;
                response.DisplayMessage = "Error";
                return response;
            }
        }


        public async Task<ResponseDto<string>> ResetPassword(string email)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByEmailAsync(email);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the userid provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var result = await _accountRepo.ForgotPassword(findUser);
                if (result == null)
                {
                    response.ErrorMessages = new List<string>() { "Error in generating reset token for user" };
                    response.StatusCode = 501;
                    response.DisplayMessage = "Error";
                    return response;
                }
                var generatePassowrd = _helperServ.GenerateRandomString(10);
                var resetPassword = new ResetPassword()
                {
                    Email = findUser.Email,
                    Password = generatePassowrd,
                    Token = result
                };

                var resetPasswordAsync = await _accountRepo.ResetPasswordAsync(findUser, resetPassword);
                if (resetPasswordAsync == null)
                {
                    response.ErrorMessages = new List<string>() { "Invalid token" };
                    response.DisplayMessage = "Error";
                    response.StatusCode = 400;
                    return response;
                }


                var body = $@"
                                <!DOCTYPE html>
                                       <html>
                                        <head>
                                        <meta charset=""UTF-8"" />
                                        <title>Gomtech Invitation</title>
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
                                            <h2>Reset Password</h2>
                                            <p>Hello {findUser.UserName},</p>
                                            <p>Reset password. Your credential are below:</p>
                                            <p>
                                             Usernamme :: {findUser.UserName},
Password :: {generatePassowrd},

                                            </p>
                                            
                                            <p>Best regards,<br/>The Team</p>
                                          </div>
                                        </body>
                                        </html>";

                var message = new Message(
                    new[] { findUser.Email },
                    "Invite User",
                    body
                );

                _emailServices.SendEmail(message);
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully reset user password";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in reset user password" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }


        public async Task<ResponseDto<string>> SuspendUserAsync(string useremail)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByEmailAsync(useremail);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the email provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                findUser.IsSuspend = true;
                var updateUser = await _accountRepo.UpdateUserInfo(findUser);
                if (updateUser == false)
                {
                    response.ErrorMessages = new List<string>() { "Error in suspending user" };
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.DisplayMessage = "Error";
                    return response;
                }

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully suspend user";
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in suspending user" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<string>> UnSuspendUserAsync(string useremail)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByEmailAsync(useremail);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the email provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                findUser.IsSuspend = false;
                var updateUser = await _accountRepo.UpdateUserInfo(findUser);
                if (updateUser == false)
                {
                    response.ErrorMessages = new List<string>() { "Error in unsuspending user" };
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.DisplayMessage = "Error";
                    return response;
                }
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully unsuspend user";
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in unsuspending user" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }

        public async Task<ResponseDto<string>> DeleteUser(string email)
        {
            var response = new ResponseDto<string>();
            try
            {
                var findUser = await _accountRepo.FindUserByEmailAsync(email);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the email provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }
                findUser.IsDeleted = true;
                findUser.DeleteDate = DateTime.UtcNow;
                var updateUser = await _accountRepo.UpdateUserInfo(findUser);
                if (updateUser == false)
                {
                    response.ErrorMessages = new List<string>() { "Error in deleting user" };
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.DisplayMessage = "Error";
                    return response;
                }
                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = "Successfully delete user";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in deleting user" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<byte[]>> ExportUserInfo(ExportUserRequest request)
        {
            var response = new ResponseDto<byte[]>();
            try
            {
                var ExpoertData = await _accountRepo.ExportUsersAsync(request);


                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = ExpoertData;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in exporting users data" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<UserGrowthMetricsDto>> GetUserGrowthMetricsAsync()
        {
            var response = new ResponseDto<UserGrowthMetricsDto>();
            try
            {
                var growthMetrics = await _accountRepo.GetUserGrowthMetricsAsync();


                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = growthMetrics;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in getting users metrics" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<DisplayFindUserDTO>> GetUserbyId(string userId)
        {
            var response = new ResponseDto<DisplayFindUserDTO>();
            try
            {
                var findUser = await _accountRepo.FindUserByIdSingleAsync(userId);
                if (findUser == null)
                {
                    response.ErrorMessages = new List<string>() { "There is no user with the id provided" };
                    response.StatusCode = 404;
                    response.DisplayMessage = "Error";
                    return response;
                }

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = findUser;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in getting user details" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }
        public async Task<ResponseDto<PaginatedUser>> GetPaginatedUserInfo(int pageNumber,
    int perPageSize,
    string? nameOrEmail,
    UserStatus? status,
    bool? isVoucherLinked)
        {
            var response = new ResponseDto<PaginatedUser>();
            try
            {
                var findUser = await _accountRepo.GetPaginatedUserInfoAsync(pageNumber, perPageSize,
                    nameOrEmail, status, isVoucherLinked);

                response.StatusCode = StatusCodes.Status200OK;
                response.DisplayMessage = "Success";
                response.Result = findUser;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                response.ErrorMessages = new List<string>() { "Error in getting paginated user details" };
                response.StatusCode = 500;
                response.DisplayMessage = "Error";
                return response;
            }
        }








    }
}
