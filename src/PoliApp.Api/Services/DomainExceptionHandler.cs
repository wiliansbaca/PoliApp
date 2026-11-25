using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PoliApp.Domain;

namespace PoliApp.Api.Services;

/// <summary>Convierte las violaciones de reglas de negocio en respuestas 409 con ProblemDetails.</summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not DomainException) return false;
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = new ProblemDetails { Status = 409, Title = "Regla de negocio", Detail = ex.Message }
        });
    }
}
