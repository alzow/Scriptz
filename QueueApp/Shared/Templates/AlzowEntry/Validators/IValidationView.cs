using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public interface IValidationView
{
    bool IsValid { get; }

    bool IsVisible { get; }

    bool Validate();

    Task<bool> ValidateAsync();

    event EventHandler<bool> ValidationChanged;
}
