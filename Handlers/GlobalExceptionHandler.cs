using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BooksProject.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (status, title) = exception switch
        {
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "The request could not be processed."),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "The database rejected the requested change."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.")
        };

        ProblemDetails problemDetails = new()
        {
            Status = status,
            Title = title,
            Detail = exception.GetBaseException().Message,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Handled: no other exception handler runs.
        return true;
    }
}
