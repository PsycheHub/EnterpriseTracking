using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace ZedSystem.Api.Extension
{
    public static class InstallLibraryExtension
    {
        public static void ConfigureLibrary(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(option =>
            {
                option.ReturnHttpNotAcceptable = true;
            }).AddXmlDataContractSerializerFormatters().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true) // Allow all origins dynamically
                        .AllowCredentials(); // Allow cookies, auth headers
                });
            });
            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "OGAVIX API",
                    Version = "v1",
                    Description = """
                        ## OGAVIX – Workforce Intelligence Platform API

                        OGAVIX tracks employee attendance, desktop activity, and productivity metrics
                        in real time. This API powers both the OGAVIX dashboard and third-party integrations.

                        ### Authentication
                        All endpoints (except `/api/account/user/login`, `/api/account/agent/user/login`,
                        and `/api/integration/token`) require a Bearer JWT.

                        **Admin/User flow:**
                        1. `POST /api/account/user/login` → receive `result.jwt`
                        2. Set header: `Authorization: Bearer {jwt}`

                        **Third-party integration flow:**
                        1. Super admin provisions credentials via `POST /api/integration/client/create`
                        2. Exchange them: `POST /api/integration/token` → receive `result.accessToken`
                        3. Set header: `Authorization: Bearer {accessToken}` (token valid 1 hour)

                        ### Scopes (third-party tokens)
                        | Scope | Accessible endpoints |
                        |---|---|
                        | `attendance` | `/api/attendance/**` |
                        | `activity` | `/api/activity/**` |
                        | `users` | `/api/account/user/**` (read-only) |
                        | `vouchers` | `/api/voucher/**` (read-only) |

                        ### Pagination
                        All paginated endpoints accept `pageNumber` (1-based) and `perPageSize`.
                        """,
                    Contact = new OpenApiContact
                    {
                        Name = "OGAVIX Support",
                        Email = "support@ogavix.com",
                        Url = new Uri("https://ogavix.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proprietary – All rights reserved"
                    }
                });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token. Obtain it from POST /api/account/user/login or POST /api/integration/token.",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }, new string[] { }
                    }
                });
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    option.IncludeXmlComments(xmlPath);
            });
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(option =>
   {
       option.SaveToken = true;
       option.RequireHttpsMetadata = false;
       option.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = configuration["JWT:ValidIssuer"],
           ValidAudience = configuration["JWT:ValidAudience"],
           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))
       };
   });

            // ThirdParty tokens carry scope claims; non-ThirdParty roles bypass the check entirely.
            services.AddAuthorization(options =>
            {
                bool HasScope(System.Security.Claims.ClaimsPrincipal user, string scope) =>
                    user.Claims.Any(c => c.Type == "scope" && c.Value == scope);

                options.AddPolicy("AttendanceRead", p => p.RequireAuthenticatedUser().RequireAssertion(
                    ctx => !ctx.User.IsInRole("ThirdParty") || HasScope(ctx.User, "attendance")));

                options.AddPolicy("ActivityRead", p => p.RequireAuthenticatedUser().RequireAssertion(
                    ctx => !ctx.User.IsInRole("ThirdParty") || HasScope(ctx.User, "activity")));

                options.AddPolicy("UsersRead", p => p.RequireAuthenticatedUser().RequireAssertion(
                    ctx => !ctx.User.IsInRole("ThirdParty") || HasScope(ctx.User, "users")));

                options.AddPolicy("VouchersRead", p => p.RequireAuthenticatedUser().RequireAssertion(
                    ctx => !ctx.User.IsInRole("ThirdParty") || HasScope(ctx.User, "vouchers")));
            });
        }
    }
}
