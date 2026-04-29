using System.Net.Mail;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Common.Security;

public static class EmailValidator
{
    public static string ValidateAndNormalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        try
        {
            var mailAddress = new MailAddress(normalizedEmail);

            if (mailAddress.Address != normalizedEmail)
            {
                throw new ValidationException("Email format is invalid.");
            }
        }
        catch (FormatException)
        {
            throw new ValidationException("Email format is invalid.");
        }

        return normalizedEmail;
    }
}