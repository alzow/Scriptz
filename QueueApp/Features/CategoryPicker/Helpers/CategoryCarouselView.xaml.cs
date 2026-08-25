using System.Collections;
using System.Windows.Input;
using QueueApp.Features.CategoryPicker.Models;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class CategoryCarouselView : ContentView
{
    public static readonly BindableProperty CategoriesProperty = BindableProperty.Create(
        nameof(Categories), typeof(IEnumerable), typeof(CategoryCarouselView), default(IEnumerable));
    public static readonly BindableProperty SelectedCategoryProperty = BindableProperty.Create(
        nameof(SelectedCategory), typeof(ServiceCategory), typeof(CategoryCarouselView), default(ServiceCategory));
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(CategoryCarouselView), default(ICommand));

    public IEnumerable Categories
    {
        get => (IEnumerable)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public ServiceCategory SelectedCategory
    {
        get => (ServiceCategory)GetValue(SelectedCategoryProperty);
        set => SetValue(SelectedCategoryProperty, value);
    }

    public ICommand SelectCommand
    {
        get => (ICommand)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public CategoryCarouselView()
    {
        InitializeComponent();
    }
}
