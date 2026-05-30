namespace MangaTracker.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "Manga Tracker";
    public bool EnableSsl { get; init; } = true;
}