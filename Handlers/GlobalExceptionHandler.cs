using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BooksProject.Handlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // SQLITE_CONSTRAINT (19): FK/unique/check violations. ExecuteDeleteAsync and
    // other raw paths throw SqliteException directly; EF wraps it in DbUpdateException.
    private static bool IsConstraintViolation(Exception exception) =>
        exception switch
        {
            SqliteException sql => sql.SqliteErrorCode == 19,
            DbUpdateException db when db.InnerException is SqliteException inner =>
                inner.SqliteErrorCode == 19,
            _ => false
        };

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
            _ when IsConstraintViolation(exception) => (
                StatusCodes.Status409Conflict,
                "The database rejected the requested change."),
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
            Detail = httpContext.RequestServices
    .GetRequiredService<IHostEnvironment>()
    .IsDevelopment()
        ? exception.GetBaseException().Message
        : "An unexpected error occurred.",
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
