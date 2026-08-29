namespace QueueApp.Shared.Templates.QueueEntry.Validators;

public class SharedStateManager : ISharedStateManager
{
    private readonly List<IValidationView> _views = new();

    public bool IsValid { get; private set; }

    public event Action<bool>? ValidationStateChanged;

    public void Register(IValidationView view)
    {
        if (view is null || _views.Contains(view))
            return;

        _views.Add(view);
        view.ValidationChanged += OnViewValidationChanged;
        Recalculate();
    }

    public void Unregister(IValidationView view)
    {
        if (view is null || !_views.Remove(view))
            return;

        view.ValidationChanged -= OnViewValidationChanged;
        Recalculate();
    }

    public bool Validate()
    {
        foreach (var view in _views)
        {
            view.Validate();
        }

        Recalculate();
        return IsValid;
    }

    private void OnViewValidationChanged(object? sender, bool isValid) => Recalculate();

    private void Recalculate()
    {
        var isValid = _views.Count > 0 && _views.All(view => !view.IsVisible || view.IsValid);

        if (isValid == IsValid)
            return;

        IsValid = isValid;
        ValidationStateChanged?.Invoke(IsValid);
    }
}
