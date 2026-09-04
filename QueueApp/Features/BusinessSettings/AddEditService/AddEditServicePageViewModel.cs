using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.BusinessSettings.AddEditService;

public partial class AddEditServicePageViewModel : BaseViewModel
{
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IIntakeFieldsService _intakeFieldsService;
    private Guid _businessId;
    private Guid? _editingServiceId;

    public AddEditServicePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService,
        IIntakeFieldsService intakeFieldsService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        _intakeFieldsService = intakeFieldsService;
    }

    public IValidator NameValidator { get; } = new RequiredValidator("Service name is required.");
    public IValidator DurationValidator { get; } = new RequiredValidator("Duration is required.");

    public string Name { get; set; } = "";
    public string DurationMinutesText { get; set; } = "";
    public string PriceRandText { get; set; } = "";
    public bool IsSaving { get; set; }
    public string PageTitle { get; set; } = "Add Service";

    // What this service asks before someone can join or book it. Empty for every service that was
    // here before this existed, and for every one that simply doesn't need to ask anything.
    public ObservableCollection<IntakeFieldResponse> IntakeFields { get; } = new();
    public bool HasNoIntakeFields => IntakeFields.Count == 0;

    // A question hangs off a service id, and a service being created doesn't have one yet.
    public bool IsEditingService => _editingServiceId is not null;
    public bool IsNewService => _editingServiceId is null;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditServicePage requires a businessId.");

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var svcObj))
            {
                _editingServiceId = (Guid)svcObj;
                PageTitle = "Edit Service";
                OnPropertyChanged(nameof(IsEditingService));
                OnPropertyChanged(nameof(IsNewService));
                await LoadExistingAsync(_editingServiceId.Value);
                await LoadIntakeFieldsAsync();
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task LoadExistingAsync(Guid serviceId)
    {
        try
        {
            var services = await _serviceOfferingsService.GetServicesAsync(_businessId);
            var existing = services.FirstOrDefault(s => s.Id == serviceId);
            if (existing is null) return;

            Name = existing.Name;
            DurationMinutesText = existing.EstMinutes.ToString();
            PriceRandText = existing.PriceCents.HasValue ? (existing.PriceCents.Value / 100m).ToString("0.##") : "";
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    // Coming back from the question editor is the only way this list changes, so it reloads here
    // rather than being handed a result.
    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();

            if (_editingServiceId is not null)
                await LoadIntakeFieldsAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public async Task LoadIntakeFieldsAsync()
    {
        try
        {
            if (_editingServiceId is null)
                return;

            var fields = await _intakeFieldsService.GetFieldsForServiceAsync(_editingServiceId.Value);

            IntakeFields.Clear();
            foreach (var field in fields.OrderBy(f => f.SortOrder))
                IntakeFields.Add(field);

            OnPropertyChanged(nameof(HasNoIntakeFields));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task AddFieldAsync()
    {
        try
        {
            if (_editingServiceId is null)
                return;

            await NavigationService.NavigateAsync(NavigationPaths.AddEditIntakeFieldPage,
                new NavigationParameters { [NavigationKeys.ServiceId] = _editingServiceId.Value });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task EditFieldAsync(IntakeFieldResponse? field)
    {
        try
        {
            if (field is null || _editingServiceId is null)
                return;

            await NavigationService.NavigateAsync(NavigationPaths.AddEditIntakeFieldPage,
                new NavigationParameters
                {
                    [NavigationKeys.ServiceId] = _editingServiceId.Value,
                    [NavigationKeys.IntakeFieldId] = field.Id,
                });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public Task MoveFieldUpAsync(IntakeFieldResponse? field) => MoveFieldAsync(field, -1);

    [RelayCommand]
    public Task MoveFieldDownAsync(IntakeFieldResponse? field) => MoveFieldAsync(field, 1);

    // Order is what the customer sees, so it is stored rather than implied: the two rows swap
    // sort_order and both are written.
    public async Task MoveFieldAsync(IntakeFieldResponse? field, int direction)
    {
        try
        {
            if (field is null)
                return;

            var index = IntakeFields.IndexOf(field);
            var target = index + direction;

            if (index < 0 || target < 0 || target >= IntakeFields.Count)
                return;

            var other = IntakeFields[target];
            var fieldOrder = field.SortOrder;
            var otherOrder = other.SortOrder;

            // Rows written by an older build can share a sort_order; renumbering from the list's
            // own order is the only way a swap means anything then.
            if (fieldOrder == otherOrder)
            {
                fieldOrder = index;
                otherOrder = target;
            }

            await _intakeFieldsService.SetFieldOrderAsync(field.Id, otherOrder);
            await _intakeFieldsService.SetFieldOrderAsync(other.Id, fieldOrder);

            field.SortOrder = otherOrder;
            other.SortOrder = fieldOrder;

            IntakeFields.Move(index, target);
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
        // The button already forces its bound loading flag true on tap, before this ever runs —
        // every exit, including a validation failure, has to go through the same finally or it
        // stays stuck until the page is left and reopened.
        IsSaving = true;
        try
        {
            if (!NameValidator.Validate(Name) || !DurationValidator.Validate(DurationMinutesText))
                return;

            if (!int.TryParse(DurationMinutesText, out var duration) || duration <= 0)
                throw new InvalidOperationException("Duration must be a whole number of minutes.");

            int? priceCents = null;
            if (!string.IsNullOrWhiteSpace(PriceRandText) && decimal.TryParse(PriceRandText, out var rand))
                priceCents = (int)Math.Round(rand * 100);

            if (_editingServiceId is null)
            {
                await _serviceOfferingsService.CreateServiceAsync(new CreateServiceRequest
                {
                    BusinessId = _businessId,
                    Name = Name,
                    EstMinutes = duration,
                    PriceCents = priceCents
                });
            }
            else
            {
                await _serviceOfferingsService.UpdateServiceAsync(_editingServiceId.Value, new UpdateServiceRequest
                {
                    Name = Name,
                    EstMinutes = duration,
                    PriceCents = priceCents
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
