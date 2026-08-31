using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Settings;

public partial class AddEditOperatorPageViewModel : BaseViewModel
{
    public IValidator NameValidator { get; } = new RequiredValidator("Name is required.");

    public string DisplayName { get; set; } = "";
    public bool IsSaving { get; set; }
    public bool IsDeactivating { get; set; }
    public string PageTitle { get; set; } = "Add Staff Member";
    public bool IsEditMode { get; set; }
    public bool IsSaveEnabled => !string.IsNullOrWhiteSpace(DisplayName) && !IsSaving;
    public bool IsDirty => DisplayName.Trim() != _originalName;

    private Guid _businessId;
    private Guid? _editingOperatorId;
    private int _sortOrder;
    private string _originalName = "";

    private readonly IOperatorService _operatorService;
    private readonly IQueuePopupService _popupService;

    public AddEditOperatorPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditOperatorPage requires a businessId.");

            var operators = await _operatorService.GetAllOperatorsForManagementAsync(_businessId);

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.OperatorId, out var opObj))
            {
                _editingOperatorId = (Guid)opObj;
                PageTitle = "Edit Staff Member";
                IsEditMode = true;

                var existing = operators.FirstOrDefault(o => o.Id == _editingOperatorId);
                if (existing is not null)
                {
                    DisplayName = existing.DisplayName;
                    _sortOrder = existing.SortOrder;
                }
            }
            else
            {
                _sortOrder = operators.Count == 0 ? 0 : operators.Max(o => o.SortOrder) + 1;
            }

            _originalName = DisplayName.Trim();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            if (IsDirty)
            {
                var discard = await _popupService.ShowConfirmAsync(
                    "Discard changes?", "You haven't saved this team member.", "Discard", "Keep editing");
                if (!discard)
                    return;
            }

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!IsSaveEnabled)
            return;

        IsSaving = true;
        try
        {
            if (_editingOperatorId is null)
            {
                await _operatorService.CreateOperatorAsync(new CreateOperatorRequest
                {
                    BusinessId = _businessId,
                    DisplayName = DisplayName.Trim(),
                    SortOrder = _sortOrder,
                });
            }
            else
            {
                await _operatorService.UpdateOperatorAsync(_editingOperatorId.Value, new UpdateOperatorRequest
                {
                    DisplayName = DisplayName.Trim(),
                    SortOrder = _sortOrder,
                });
            }

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task DeactivateAsync()
    {
        if (_editingOperatorId is null)
            return;

        IsDeactivating = true;
        try
        {
            var confirmed = await _popupService.ShowConfirmAsync(
                "Take this person off the team?",
                $"{DisplayName.Trim()} won't appear on the queue board. You can bring them back at any time.",
                "Deactivate", "Keep on team");
            if (!confirmed)
                return;

            await _operatorService.SetOperatorActiveAsync(_editingOperatorId.Value, false);
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsDeactivating = false;
        }
    }
}
