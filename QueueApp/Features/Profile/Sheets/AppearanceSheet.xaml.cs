using CommunityToolkit.Mvvm.Input;
using QueueApp.Framework.Theming;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.Profile.Sheets;

public partial class AppearanceSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popupService;
    private readonly TaskCompletionSource<bool> _completion = new();

    public IRelayCommand<string> SetThemeCommand { get; }

    public Task<bool> Completion => _completion.Task;

    public string VersionText { get; }

    public bool IsSystemTheme { get; private set; }
    public bool IsLightTheme { get; private set; }
    public bool IsDarkTheme { get; private set; }

    public AppearanceSheet() : this(null!)
    {
    }

    public AppearanceSheet(IQueuePopupService popupService)
    {
        _popupService = popupService;
        SetThemeCommand = new RelayCommand<string>(SetTheme);
        VersionText = $"Queue {AppInfo.Current.VersionString}";

        SyncSelection();
        InitializeComponent();

        ThemeService.ThemeChanged += OnThemeChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ThemeService.ThemeChanged -= OnThemeChanged;
        _completion.TrySetResult(true);
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        MainThread.BeginInvokeOnMainThread(() => BackgroundColor = ThemePalette.Scrim);

    private void SetTheme(string? choice)
    {
        if (!Enum.TryParse<ThemeChoice>(choice, out var parsed))
            return;

        ThemeService.Set(parsed);
        SyncSelection();

        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    private void SyncSelection()
    {
        IsSystemTheme = ThemeService.Current == ThemeChoice.System;
        IsLightTheme = ThemeService.Current == ThemeChoice.Light;
        IsDarkTheme = ThemeService.Current == ThemeChoice.Dark;
    }

    private async void OnCloseClicked(object sender, EventArgs e) => await _popupService.HideSheetAsync(this);
}
