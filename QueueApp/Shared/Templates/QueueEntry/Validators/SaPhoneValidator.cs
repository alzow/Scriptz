namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public class SaPhoneValidator : IValidator
{
    public bool IsAsync { get; set; } = false;
    public bool IsBlocking { get; set; } = true;
    public string ErrorMessage { get; set; }

    public SaPhoneValidator(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public bool Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        return (digits.Length == 10 && digits[0] == '0')
            || (digits.Length == 11 && digits.StartsWith("27"))
            || digits.Length == 9;
    }
}
