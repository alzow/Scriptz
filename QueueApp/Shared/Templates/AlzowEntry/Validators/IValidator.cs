using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public interface IValidator
{
    bool IsBlocking { get; set; }
    bool IsAsync { get; set; }
    string ErrorMessage { get; set; }
    bool Validate(string value);

    bool Validate(DateTime value)
    { return false; }

    Task<bool> ValidateAsync(string value)
    {
        return Task.FromResult(false);
    }

    void SetErrorState(bool isValid, string errorMessage)
    {
        //default implementation
    }
}
