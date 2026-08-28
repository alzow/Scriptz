namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public interface ISharedStateManager
{
    bool IsValid { get; }

    event Action<bool>? ValidationStateChanged;

    void Register(IValidationView view);

    void Unregister(IValidationView view);

    bool Validate();
}
