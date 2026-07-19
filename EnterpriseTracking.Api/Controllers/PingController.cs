using EnterpriseTracking.Core.Dto.Request.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            return Ok();
        }
    }
}
