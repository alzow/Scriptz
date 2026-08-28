namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public class EmailValidator : IValidator
{
    public bool IsAsync { get; set; } = false;
    public bool IsBlocking { get; set; } = true;
    public string ErrorMessage { get; set; }

    public EmailValidator(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public bool Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@');

        if (at <= 0 || at == trimmed.Length - 1)
            return false;

        if (trimmed.IndexOf('@', at + 1) >= 0)
            return false;

        var domain = trimmed[(at + 1)..];

        return domain.Contains('.')
            && !domain.StartsWith('.')
            && !domain.EndsWith('.');
    }
}
