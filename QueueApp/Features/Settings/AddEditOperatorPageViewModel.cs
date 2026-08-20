using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.AlzowEntry.Validators;

namespace QueueApp.Features.Settings;

public partial class AddEditOperatorPageViewModel : BaseViewModel
{
    private readonly IOperatorService _operatorService;
    private Guid _businessId;
    private Guid? _editingOperatorId;

    public AddEditOperatorPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        FormStateManager.ValidationStateChanged += isValid => IsFormValid = isValid;
    }

    public ISharedStateManager FormStateManager { get; } = new SharedStateManager();
    public IValidator NameValidator { get; } = new RequiredValidator("Name is required.");

    public string DisplayName { get; set; } = "";
    public string SortOrderText { get; set; } = "0";
    public bool IsFormValid { get; set; }
    public bool IsSaving { get; set; }
    public string PageTitle { get; set; } = "Add Staff Member";

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue("businessId", out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditOperatorPage requires a businessId.");

            if (parameters is not null && parameters.TryGetValue("operatorId", out var opObj))
            {
                _editingOperatorId = (Guid)opObj;
                PageTitle = "Edit Staff Member";
                await LoadExistingAsync(_editingOperatorId.Value);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private async Task LoadExistingAsync(Guid operatorId)
    {
        var operators = await _operatorService.GetAllOperatorsForManagementAsync(_businessId);
        var existing = operators.FirstOrDefault(o => o.Id == operatorId);
        if (existing is null) return;

        DisplayName = existing.DisplayName;
        SortOrderText = existing.SortOrder.ToString();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var sortOrder = int.TryParse(SortOrderText, out var parsed) ? parsed : 0;

            if (_editingOperatorId is null)
            {
                await _operatorService.CreateOperatorAsync(new CreateOperatorRequest
                {
                    BusinessId = _businessId,
                    DisplayName = DisplayName,
                    SortOrder = sortOrder
                });
            }
            else
            {
                await _operatorService.UpdateOperatorAsync(_editingOperatorId.Value, new UpdateOperatorRequest
                {
                    DisplayName = DisplayName,
                    SortOrder = sortOrder
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
}
