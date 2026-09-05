using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.IntakeAnswers.Constants;
using QueueApp.Features.IntakeAnswers.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.IntakeAnswers;

// Read-only, from the shop's side: the questions this service asked and what the customer wrote
// back. Filling or correcting an answer as the operator is a different feature — it needs the field
// definitions rather than the stored snapshot, and a write path this page deliberately has not got.
public partial class IntakeAnswersPageViewModel : BaseViewModel
{
    public string PageTitle => IntakeAnswersConstants.PageTitle;
    public string SectionTitle => IntakeAnswersConstants.SectionTitle;
    public string EmptyText => IntakeAnswersConstants.EmptyText;

    public string CustomerName { get; set; } = string.Empty;
    public string SubtitleText { get; set; } = string.Empty;
    public bool IsOpeningFile { get; set; }

    public ObservableCollection<IntakeAnswer> Answers { get; } = new();
    public bool HasAnswers => Answers.Count > 0;
    public bool ShowEmpty => Answers.Count == 0;

    private readonly IIntakeFileService _intakeFileService;
    private readonly IQueuePopupService _popupService;

    public IntakeAnswersPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IIntakeFileService intakeFileService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _intakeFileService = intakeFileService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            if (parameters is null
                || !parameters.TryGetValue(NavigationKeys.IntakeAnswers, out var snapshotObj)
                || snapshotObj is not IntakeAnswerSnapshot snapshot)
                return;

            Apply(snapshot);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void Apply(IntakeAnswerSnapshot snapshot)
    {
        try
        {
            CustomerName = snapshot.CustomerName;
            SubtitleText = IntakeAnswersHelper.BuildSubtitle(snapshot);

            Answers.Clear();
            foreach (var answer in snapshot.Answers)
                Answers.Add(answer);

            OnPropertyChanged(nameof(HasAnswers));
            OnPropertyChanged(nameof(ShowEmpty));
        }
        catch (Exception ex)
        {
            _ = HandleExceptionAsync(ex);
        }
    }

    // The operator reads the file through the bucket's business-read policy rather than the
    // customer's own-read one, so this is the one thing on the page that can fail on authorisation
    // alone with everything else on screen rendering fine.
    [RelayCommand]
    public async Task OpenFileAsync(IntakeAnswer? answer)
    {
        try
        {
            if (IsOpeningFile || answer?.File is not { } file)
                return;

            IsOpeningFile = true;

            var downloadedPath = await _intakeFileService.DownloadAsync(file);
            await Launcher.Default.OpenAsync(new OpenFileRequest(
                file.Name,
                new ReadOnlyFile(downloadedPath, file.ContentType ?? IntakeAnswersConstants.DefaultContentType)));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsOpeningFile = false;
        }
    }

    public override bool TryHandleSystemBack()
    {
        GoBackCommand.Execute(null);
        return true;
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

    protected override async Task HandleExceptionAsync(Exception exception)
    {
        await base.HandleExceptionAsync(exception);

        try
        {
            await _popupService.ShowAlertAsync("Couldn't do that", GetFriendlyErrorMessage(exception));
        }
        catch (Exception)
        {
            // No page to show it on. base.HandleExceptionAsync is the whole record of it.
        }
    }
}
