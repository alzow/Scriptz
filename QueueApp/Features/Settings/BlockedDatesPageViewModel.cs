using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class BlockedDatesPageViewModel : BaseViewModel
{
    private readonly IOperatorService _operatorService;
    private Guid _operatorId;

    public BlockedDatesPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        Title = "Blocked Dates";
    }

    public const int SkeletonRowCount = 5;

    public ObservableCollection<AvailabilityBlockResponse> Blocks { get; } = new();
    public string OperatorName { get; set; } = "";
    public bool IsEmpty => Blocks.Count == 0 && !IsFirstLoading && !HasLoadFailed;

    public override void OnLoadStateChanged() => OnPropertyChanged(nameof(IsEmpty));

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _operatorId = parameters is not null && parameters.TryGetValue(NavigationKeys.OperatorId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("BlockedDatesPage requires an operatorId.");

            OperatorName = parameters!.TryGetValue(NavigationKeys.OperatorName, out var nameObj) ? (string)nameObj : "";

            await RunFirstLoadAsync(FetchAsync);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        if (_operatorId != Guid.Empty)
            await LoadAsync();
    }

    public override Task ReloadAsync() => RunFirstLoadAsync(FetchAsync);

    // Throws so RunFirstLoadAsync can turn the skeleton into a failure instead of leaving it up.
    public async Task FetchAsync()
    {
        var blocks = await _operatorService.GetAvailabilityBlocksAsync(_operatorId);
        Blocks.Clear();
        foreach (var b in blocks)
            Blocks.Add(b);

        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            await FetchAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task AddBlockAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.AddAvailabilityBlockPage,
            new NavigationParameters { [NavigationKeys.OperatorId] = _operatorId });
    }

    [RelayCommand]
    private async Task DeleteBlockAsync(AvailabilityBlockResponse block)
    {
        block.IsDeleting = true;
        try
        {
            await _operatorService.DeleteAvailabilityBlockAsync(block.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            block.IsDeleting = false;
        }
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
}
