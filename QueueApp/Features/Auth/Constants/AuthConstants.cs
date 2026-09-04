namespace QueueApp.Features.Auth.Constants;

public static class AuthConstants
{
    public const int PasswordMinimumLength = 6;

    public const string SignInHeading = "Welcome back";
    public const string SignInLead = "Pick up where you left off.";

    public const string SignUpHeading = "Create your account";
    public const string SignUpLead = "So we can hold your place and tell you when to leave.";
    public const string Terms = "By continuing you agree to our terms and privacy policy.";

    public const string InvalidCredentialsMessage = "That email and password don't match an account.";
    public const string EmailNotConfirmedMessage =
        "Confirm your email address first — check your inbox for the link.";
    public const string EmailTakenMessage = "That email is already registered. Try signing in instead.";
    public const string PhoneTakenMessage =
        "That mobile number is already registered. Try signing in instead.";
    public const string ShortPasswordMessage = "Your password is too short.";
    public const string BadEmailMessage = "That email address doesn't look right.";
    public const string RateLimitedMessage = "Too many attempts. Wait a minute and try again.";
    public const string OfflineMessage = "No connection. Check your internet and try again.";
    public const string SignInFailureMessage = "Couldn't sign you in. Please try again.";
    public const string SignUpFailureMessage = "Couldn't create your account. Please try again.";
    public const string ConfirmEmailMessage =
        "Account created. Check your email to confirm your address, then sign in.";

    public const string InvalidEmailValidation = "Enter a valid email address.";
    public const string MissingPasswordValidation = "Enter your password.";
}
