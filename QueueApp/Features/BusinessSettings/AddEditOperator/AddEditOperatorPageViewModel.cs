using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.BusinessSettings.AddEditOperator;

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
    }

    public IValidator NameValidator { get; } = new RequiredValidator("Name is required.");

    public string DisplayName { get; set; } = "";
    public string SortOrderText { get; set; } = "0";
    public bool IsSaving { get; set; }
    public string PageTitle { get; set; } = "Add Staff Member";

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditOperatorPage requires a businessId.");

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.OperatorId, out var opObj))
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

    public async Task LoadExistingAsync(Guid operatorId)
    {
        try
        {
            var operators = await _operatorService.GetAllOperatorsForManagementAsync(_businessId);
            var existing = operators.FirstOrDefault(o => o.Id == operatorId);
            if (existing is null) return;

            DisplayName = existing.DisplayName;
            SortOrderText = existing.SortOrder.ToString();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.GoBackAsync());
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!NameValidator.Validate(DisplayName))
        {
            return;
        }

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

            await RunNavigationAsync(() => NavigationService.GoBackAsync());
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
