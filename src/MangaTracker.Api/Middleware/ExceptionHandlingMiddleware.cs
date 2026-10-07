using System.Diagnostics;
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away (closed the tab, navigated): nobody reads the response
            // and it is not a server error, so it is not logged as one.
            _logger.LogDebug("Request {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception exception) when (context.Response.HasStarted)
        {
            // Status and headers are already sent; a ProblemDetails can no longer be written.
            _logger.LogError(exception, "An error occurred after the response had started.");
            throw;
        }
        catch (ValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation error", exception.Message);
        }
        catch (NotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Resource not found", exception.Message);
        }
        catch (ConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Conflict", exception.Message);
        }
        catch (ExternalServiceUnavailableException exception)
        {
            // The cause stays in the logs; the client only learns it can retry later.
            _logger.LogWarning(exception.InnerException, "External service unavailable: {Message}", exception.Message);
            context.Response.Headers.RetryAfter = "30";

            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "Service unavailable", exception.Message);
        }
        catch (DomainException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Domain error", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected error occurred.");

            // The message of an unexpected exception may hold internal details, so the
            // client gets a generic text and the trace id to match it with the logs.
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        // WriteAsJsonAsync would otherwise set application/json and drop the ProblemDetails type.
        await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
    }
}
