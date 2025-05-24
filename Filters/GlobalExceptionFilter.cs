using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Filters;

public sealed class GlobalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;

        var isBadParam = ex is ArgumentException or ArgumentNullException or FormatException;

        var status = isBadParam
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var errorCode = isBadParam ? "BadRequest" : "InternalServerError";

        var payload = new
        {
            error = new
            {
                code    = errorCode,
                message = ex.Message
            }
        };

        context.Result = new JsonResult(payload) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
