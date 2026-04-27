using MangaTracker.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException exception)
        {
            await HandleDomainExceptionAsync(context, exception);
        }
        catch (Exception exception)
        {
            await HandleUnexpectedExceptionAsync(context, exception);
        }
    }

    private static async Task HandleDomainExceptionAsync(
        HttpContext context,
        DomainException exception)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;

        var problemDetails = new ProblemDetails
        {
            Title = "Domain error",
            Status = StatusCodes.Status409Conflict,
            Detail = exception.Message
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private async Task HandleUnexpectedExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        _logger.LogError(exception, "An unexpected error occurred.");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Title = "Unexpected server error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred."
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}