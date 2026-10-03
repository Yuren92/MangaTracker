using MangaTracker.Application.Common.Exceptions;
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
        catch (ValidationException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                StatusCodes.Status400BadRequest,
                "Validation error");
        }
        catch (NotFoundException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                StatusCodes.Status404NotFound,
                "Resource not found");
        }
        catch (ConflictException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                StatusCodes.Status409Conflict,
                "Conflict");
        }
        catch (ExternalServiceUnavailableException exception)
        {
            // The cause stays in the logs; the client only learns it can retry later.
            _logger.LogWarning(exception.InnerException, "External service unavailable: {Message}", exception.Message);
            context.Response.Headers.RetryAfter = "30";

            await HandleExceptionAsync(
                context,
                exception,
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable");
        }
        catch (DomainException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                StatusCodes.Status400BadRequest,
                "Domain error");
        }
        catch (Exception exception)
        {
            await HandleUnexpectedExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string title)
    {
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = exception.Message
        };

        // WriteAsJsonAsync would otherwise set application/json and drop the ProblemDetails type.
        await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
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

        // WriteAsJsonAsync would otherwise set application/json and drop the ProblemDetails type.
        await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
    }
}