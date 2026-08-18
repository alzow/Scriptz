using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessList;

public partial class BusinessListPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private string _category = "";

    public BusinessListPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
    }

    public ObservableCollection<BusinessResponse> Businesses { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => Businesses.Count == 0 && !IsLoading;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _category = parameters is not null && parameters.TryGetValue("category", out var catObj)
                ? (string)catObj
                : throw new InvalidOperationException("BusinessListPage requires a 'category' parameter.");

            Title = CategoryPicker.CategoryCatalog.All.First(c => c.Key == _category).Display;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _businessService.GetBusinessesAsync(_category);
            Businesses.Clear();
            foreach (var b in results.OrderByDescending(b => b.IsAvailableNow))
                Businesses.Add(b);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            var ownsBusiness = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness);
            await NavigationService.NavigateAsync(uri);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    private async Task OpenBusinessAsync(BusinessResponse business)
    {
        try
        {
            var navParams = new NavigationParameters { ["businessId"] = business.Id };
            await NavigationService.NavigateAsync(NavigationPaths.BusinessDetailPage, navParams);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
