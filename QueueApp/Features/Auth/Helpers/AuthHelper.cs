using Refit;
using QueueApp.Features.Auth.Constants;

namespace QueueApp.Features.Auth.Helpers;

// GoTrue reports what went wrong in the response body rather than the status code, and the wording
// it uses is not wording to put in front of a customer. These map the body to copy that is.
public static class AuthHelper
{
    public static string TranslateSignInError(ApiException exception)
    {
        var body = exception.Content ?? string.Empty;

        if (Mentions(body, "Email not confirmed", "email_not_confirmed"))
            return AuthConstants.EmailNotConfirmedMessage;

        if (Mentions(body, "Invalid login credentials", "invalid_credentials")
            || exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return AuthConstants.InvalidCredentialsMessage;

        if (Mentions(body, "rate limit")
            || exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            return AuthConstants.RateLimitedMessage;

        return AuthConstants.SignInFailureMessage;
    }

    public static string TranslateSignUpError(ApiException exception)
    {
        var body = exception.Content ?? string.Empty;

        if (Mentions(body, "already registered", "already been registered", "user_already_exists"))
            return AuthConstants.EmailTakenMessage;

        if (Mentions(body, "Password should be at least", "weak_password"))
            return AuthConstants.ShortPasswordMessage;

        if (Mentions(body, "invalid format", "Unable to validate email"))
            return AuthConstants.BadEmailMessage;

        if (Mentions(body, "rate limit")
            || exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            return AuthConstants.RateLimitedMessage;

        return AuthConstants.SignUpFailureMessage;
    }

    private static bool Mentions(string body, params string[] needles) =>
        needles.Any(n => body.Contains(n, StringComparison.OrdinalIgnoreCase));
}
