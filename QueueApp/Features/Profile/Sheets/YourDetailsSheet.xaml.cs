using QueueApp.Services.Api.Profile;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Profile.Sheets;

public partial class YourDetailsSheet : BottomSheetPage
{
    private readonly IProfileService _profileService;
    private readonly IQueuePopupService _popupService;
    private readonly Guid _userId;
    private readonly TaskCompletionSource<bool> _completion = new();

    private bool _saved;

    public IValidator NameValidator { get; } = new RequiredValidator("Enter the name shops should see.");
    public IValidator PhoneValidator { get; } = new SaPhoneValidator("Enter a valid SA mobile number.");

    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; }
    public string ErrorMessage { get; private set; } = "";
    public bool IsSaving { get; private set; }

    public Task<bool> Completion => _completion.Task;

    public YourDetailsSheet() : this(null!, null!, Guid.Empty, "", "", "")
    {
    }

    public YourDetailsSheet(
        IProfileService profileService,
        IQueuePopupService popupService,
        Guid userId,
        string name,
        string phone,
        string email)
    {
        _profileService = profileService;
        _popupService = popupService;
        _userId = userId;
        Name = name;
        Phone = phone;
        Email = email;

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(_saved);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (IsSaving)
            return;

        if (!NameValidator.Validate(Name))
        {
            SetError(NameValidator.ErrorMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Phone) && !PhoneValidator.Validate(Phone))
        {
            SetError(PhoneValidator.ErrorMessage);
            return;
        }

        SetError("");
        IsSaving = true;
        OnPropertyChanged(nameof(IsSaving));

        try
        {
            var phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
            await _profileService.UpdateMyProfileAsync(_userId, Name.Trim(), phone);
            _saved = true;
            await _popupService.HideSheetAsync(this);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsSaving = false;
            OnPropertyChanged(nameof(IsSaving));
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(ErrorMessage));
    }
}
