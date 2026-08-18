using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.CategoryPicker;

public partial class CategoryPickerPageViewModel : BaseViewModel
{
    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
    }

    public IReadOnlyList<ServiceCategory> Categories { get; } = CategoryCatalog.All;

    [RelayCommand]
    private async Task SelectCategoryAsync(ServiceCategory category)
    {
        if (!category.Available)
            return; // "coming soon" categories no-op

        try
        {
            var navParams = new NavigationParameters { ["category"] = category.Key };
            await NavigationService.NavigateAsync(NavigationPaths.BusinessListPage, navParams);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
