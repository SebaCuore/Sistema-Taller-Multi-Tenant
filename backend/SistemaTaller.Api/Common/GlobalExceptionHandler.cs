using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SistemaTaller.Api.Common;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Error no manejado: {Mensaje}", exception.Message);

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error inesperado",
        };

        context.Response.StatusCode = problema.Status.Value;
        await context.Response.WriteAsJsonAsync(problema, ct);
        return true;
    }
}
