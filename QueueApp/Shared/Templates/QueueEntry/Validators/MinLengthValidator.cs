namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public class MinLengthValidator : IValidator
{
    private readonly int _minimumLength;

    public bool IsAsync { get; set; } = false;
    public bool IsBlocking { get; set; } = true;
    public string ErrorMessage { get; set; }

    public MinLengthValidator(int minimumLength, string errorMessage)
    {
        _minimumLength = minimumLength;
        ErrorMessage = errorMessage;
    }

    public bool Validate(string value)
        => !string.IsNullOrEmpty(value) && value.Length >= _minimumLength;
}
