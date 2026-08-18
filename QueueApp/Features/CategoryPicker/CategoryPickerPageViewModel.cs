using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Messages;
using QueueApp.Services.Storage;

namespace QueueApp.Features.CategoryPicker;

public partial class CategoryPickerPageViewModel : BaseViewModel
{
    private readonly IMessenger _messenger;

    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IMessenger messenger)
        : base(navigationService, secureStorageService)
    {
        _messenger = messenger;
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
            _messenger.Send(new NavigateAwayFromTabsMessage($"/NavigationPage/{NavigationPaths.BusinessListPage}", navParams, true));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
