using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.BusinessSettings.Constants;
using QueueApp.Features.BusinessSettings.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.BusinessSettings.IntakeFormPreview;

// The configured questions, rendered by the same view the customer answers. Nothing is submitted
// and nothing is stored: this exists so an owner can see the finished form without a customer
// having to exist first.
public partial class IntakeFormPreviewPageViewModel : BaseViewModel
{
    public ObservableCollection<IntakeFieldItem> Fields { get; } = new();

    public bool HasFields => Fields.Count > 0;
    public string SummaryLine { get; set; } = string.Empty;
    public string CostLine { get; set; } = string.Empty;

    // Every question is shown, conditional ones included — the owner is checking the shape of the
    // form, not walking one customer's path through it.
    public bool HasConditional { get; set; }

    private Guid _serviceId;

    private readonly IIntakeFieldsService _intakeFieldsService;

    public IntakeFormPreviewPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IIntakeFieldsService intakeFieldsService)
        : base(navigationService, secureStorageService)
    {
        _intakeFieldsService = intakeFieldsService;
        Title = BusinessSettingsConstants.PreviewTitle;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _serviceId = parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var serviceObj)
                ? (Guid)serviceObj
                : throw new InvalidOperationException("IntakeFormPreviewPage requires a serviceId.");

            await LoadAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            var fields = (await _intakeFieldsService.GetFieldsForServiceAsync(_serviceId))
                .OrderBy(f => f.SortOrder)
                .ToList();

            Fields.Clear();
            foreach (var field in fields)
                Fields.Add(IntakeFieldItem.From(field));

            SummaryLine = IntakeQuestionHelper.SummaryLine(fields);
            CostLine = IntakeQuestionHelper.CostLine(fields.Count);
            HasConditional = fields.Any(f => f.VisibilityRule is not null);

            OnPropertyChanged(nameof(HasFields));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    // The preview lets an owner tap through the form so it feels like the real thing. Selection is
    // the only interaction that has to be wired up; nothing here is read back.
    [RelayCommand]
    public void SelectOption(IntakeOptionItem? option)
    {
        try
        {
            if (option is null)
                return;

            var owner = Fields.FirstOrDefault(f => f.Options.Contains(option));
            if (owner is null)
                return;

            if (owner.IsSingleSelect)
            {
                foreach (var candidate in owner.Options)
                    candidate.IsSelected = ReferenceEquals(candidate, option);
            }
            else
            {
                option.IsSelected = !option.IsSelected;
            }

            owner.NotifyAnswerChanged();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
