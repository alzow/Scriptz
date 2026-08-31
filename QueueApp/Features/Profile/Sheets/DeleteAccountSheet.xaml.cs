using QueueApp.Features.Profile.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.Profile.Sheets;

public partial class DeleteAccountSheet : BottomSheetPage
{
    private const string ConfirmWord = "DELETE";

    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    private readonly TaskCompletionSource<bool> _completion = new();

    private string _confirmText = "";
    private bool _deleted;

    public List<DeleteConsequenceItem> Consequences { get; }
    public bool HasCommitments => Consequences.Count > 0;

    public string ConfirmText
    {
        get => _confirmText;
        set
        {
            if (_confirmText == value)
                return;

            _confirmText = value;
            OnPropertyChanged(nameof(ConfirmText));
            OnPropertyChanged(nameof(CanDelete));
        }
    }

    public bool CanDelete => !IsDeleting && string.Equals(ConfirmText?.Trim(), ConfirmWord, StringComparison.Ordinal);
    public bool IsDeleting { get; private set; }
    public string ErrorMessage { get; private set; } = "";

    public Task<bool> Completion => _completion.Task;

    public DeleteAccountSheet() : this(null!, null!, new List<DeleteConsequenceItem>())
    {
    }

    public DeleteAccountSheet(
        IAuthService authService,
        IQueuePopupService popupService,
        List<DeleteConsequenceItem> consequences)
    {
        _authService = authService;
        _popupService = popupService;
        Consequences = consequences;

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(_deleted);
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (!CanDelete)
            return;

        SetDeleting(true);
        SetError("");

        try
        {
            await _authService.DeleteAccountAsync();
            _deleted = true;
            await _popupService.HideSheetAsync(this);
        }
        catch (Exception ex)
        {
            SetError($"We couldn't delete your account. {ex.Message}");
        }
        finally
        {
            SetDeleting(false);
        }
    }

    private async void OnKeepClicked(object sender, EventArgs e) => await _popupService.HideSheetAsync(this);

    private void SetDeleting(bool isDeleting)
    {
        IsDeleting = isDeleting;
        OnPropertyChanged(nameof(IsDeleting));
        OnPropertyChanged(nameof(CanDelete));
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(ErrorMessage));
    }
}
