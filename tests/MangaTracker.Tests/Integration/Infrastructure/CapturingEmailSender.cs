using System.Collections.Concurrent;
using MangaTracker.Application.Abstractions.Email;

namespace MangaTracker.Tests.Integration.Infrastructure;

// Replaces SMTP in integration tests and keeps the links that would have been emailed,
// so tests can follow the confirmation and password reset flows.
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    public Task SendEmailConfirmationAsync(string to, string confirmationUrl, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.Confirmation, to, confirmationUrl));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string to, string resetPasswordUrl, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.PasswordReset, to, resetPasswordUrl));
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string to, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.Welcome, to, Url: null));
        return Task.CompletedTask;
    }

    public string LatestTokenFor(string email, EmailKind kind)
    {
        var url = _sent.Last(sent =>
            sent.Kind == kind &&
            string.Equals(sent.To, email, StringComparison.OrdinalIgnoreCase)).Url!;

        var query = new Uri(url).Query.TrimStart('?');
        var token = query
            .Split('&')
            .Select(pair => pair.Split('=', 2))
            .Single(pair => pair[0] == "token")[1];

        return Uri.UnescapeDataString(token);
    }

    public int CountFor(string email, EmailKind kind)
    {
        return _sent.Count(sent =>
            sent.Kind == kind &&
            string.Equals(sent.To, email, StringComparison.OrdinalIgnoreCase));
    }
}

public enum EmailKind
{
    Confirmation,
    PasswordReset,
    Welcome
}

public sealed record SentEmail(EmailKind Kind, string To, string? Url);
