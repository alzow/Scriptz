using QueueApp.Framework.Base;
using QueueApp.Services.Storage;

namespace QueueApp.Features.CategoryPicker;

public class CategoryPickerPageViewModel : BaseViewModel
{
    public CategoryPickerPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
    }
}
