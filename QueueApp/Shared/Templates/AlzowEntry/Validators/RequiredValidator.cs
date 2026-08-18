using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public class RequiredValidator : IValidator
{
    public bool IsAsync { get; set; } = false;
    public string ErrorMessage { get; set; }
    public bool IsBlocking { get; set; } = true;

    public RequiredValidator(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public bool Validate(string value)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
    }
}
