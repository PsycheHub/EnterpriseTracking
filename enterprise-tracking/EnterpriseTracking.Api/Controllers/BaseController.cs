using EnterpriseTracking.Core.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTracking.Api.Controllers.Base
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected IActionResult HandleResponse<T>(ResponseDto<T> result)
        {
            return result.StatusCode switch
            {
                200 => Ok(result),
                201 => Created(string.Empty, result),
                404 => NotFound(result),
                400 => BadRequest(result),
                _ => StatusCode(result.StatusCode, result)
            };
        }
    }
}