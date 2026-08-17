using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Storage;
using System.Collections.ObjectModel;

namespace ScriptzApp.Features.Shop;

public partial class ShopPageViewModel : BaseViewModel
{
    private static readonly List<OtcProduct> AllProducts = new()
    {
        new("Pain Relief",  "💊", "Panado 500mg",         45,  "24 tabs",  null),
        new("Allergy",      "🌿", "Allergex Syrup",        68,  "100ml",    "Popular"),
        new("Baby",         "👶", "Infacare NAN 1",        189, "400g",     null),
        new("Vitamins",     "⚡", "Viral Choice Vit C",   129, "30 caps",  "Sale"),
        new("Skin",         "🧴", "Cetaphil Moisturiser", 185, "250ml",    null),
        new("Digestive",    "🌀", "Buscopan 10mg",         89,  "20 tabs",  null),
    };

    public ObservableCollection<string> Categories { get; } = new()
        { "All", "Pain Relief", "Allergy", "Baby", "Vitamins", "Skin", "Digestive" };

    public ObservableCollection<OtcProduct> FilteredProducts { get; } = new();

    [ObservableProperty] private string _selectedCategory = "All";
    [ObservableProperty] private bool _hasCartItems;
    [ObservableProperty] private string _viewCartText = "View Cart";

    private readonly Dictionary<string, int> _cart = new();

    public ShopPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
        Title = "Shop";
        // ApplyFilter();
    }

    // partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    // [RelayCommand]
    // private void SelectCategory(string category)
    // {
    //     SelectedCategory = category;
    // }

    // [RelayCommand]
    // private void AddToCart(OtcProduct product)
    // {
    //     if (!_cart.ContainsKey(product.Name)) _cart[product.Name] = 0;
    //     _cart[product.Name]++;
    //     product.CartQty = _cart[product.Name].ToString();
    //     product.CartButtonBg = "#E8621A";
    //     product.CartButtonTextColor = "#FFFFFF";

    //     var total = _cart.Values.Sum();
    //     HasCartItems = total > 0;
    //     ViewCartText = $"View Cart ({total} items) →";
    // }

    // [RelayCommand]
    // private void ViewCart() { }

    // private void ApplyFilter()
    // {
    //     FilteredProducts.Clear();
    //     var filtered = SelectedCategory == "All"
    //         ? AllProducts
    //         : AllProducts.Where(p => p.Category == SelectedCategory).ToList();
    //     foreach (var p in filtered)
    //         FilteredProducts.Add(p);
    // }
}

public partial class OtcProduct : ObservableObject
{
    public string Category { get; }
    public string Icon { get; }
    public string Name { get; }
    public string PriceText { get; }
    public string Qty { get; }
    public string? Badge { get; }
    public bool HasBadge => Badge != null;
    public string BadgeBg => Badge == "Sale" ? "#FDECEA" : "#E8F5F3";
    public string BadgeColor => Badge == "Sale" ? "#C0392B" : "#1A7A6E";

    [ObservableProperty] private string _cartQty = "+";
    [ObservableProperty] private string _cartButtonBg = "#F2EBE0";
    [ObservableProperty] private string _cartButtonTextColor = "#2A1A0E";

    public OtcProduct(string category, string icon, string name, decimal price, string qty, string? badge)
    {
        Category = category;
        Icon = icon;
        Name = name;
        PriceText = $"R{price}";
        Qty = qty;
        Badge = badge;
    }
}
