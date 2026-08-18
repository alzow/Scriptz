using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public interface ISharedStateManager
{
    event Action<bool> ValidationStateChanged;

    void RegisterEntry(IValidationView entry);

    void UnregisterEntry(IValidationView entry);

    void ValidateOnViewUpdate();

    void ResetValidationStatuses();
}
