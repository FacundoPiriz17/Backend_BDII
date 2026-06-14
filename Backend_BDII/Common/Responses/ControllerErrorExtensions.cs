using Microsoft.AspNetCore.Mvc;

namespace Backend_BDII.Common.Responses;
//Esta clase permite mostrar los errores de mejor manera, haciendo que no sea tan frustrante tener que buscar en donde está cuando se esté debuggeando el código.
public static class ControllerErrorExtensions
{
    public static ObjectResult ApiError(
        this ControllerBase controller,
        int statusCode,
        string code,
        string message,
        object? details = null)
    {
        return controller.StatusCode(statusCode, new ApiErrorResponse
        {
            Code = code,
            Message = message,
            Details = details
        });
    }

    public static ObjectResult BadRequestError(this ControllerBase controller, string message, string code = "bad_request")
    {
        return controller.ApiError(StatusCodes.Status400BadRequest, code, message);
    }

    public static ObjectResult UnauthorizedError(this ControllerBase controller, string message, string code = "unauthorized")
    {
        return controller.ApiError(StatusCodes.Status401Unauthorized, code, message);
    }

    public static ObjectResult ForbiddenError(this ControllerBase controller, string message, string code = "forbidden")
    {
        return controller.ApiError(StatusCodes.Status403Forbidden, code, message);
    }

    public static ObjectResult NotFoundError(this ControllerBase controller, string message, string code = "not_found")
    {
        return controller.ApiError(StatusCodes.Status404NotFound, code, message);
    }
}
