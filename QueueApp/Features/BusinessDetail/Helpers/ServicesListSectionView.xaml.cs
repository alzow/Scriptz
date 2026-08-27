using System.Collections;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class ServicesListSectionView : ContentView
{
    public static readonly BindableProperty HasServicesProperty = BindableProperty.Create(
        nameof(HasServices), typeof(bool), typeof(ServicesListSectionView), false);

    public static readonly BindableProperty ServicesCountTextProperty = BindableProperty.Create(
        nameof(ServicesCountText), typeof(string), typeof(ServicesListSectionView), string.Empty);

    public static readonly BindableProperty ServiceRowsProperty = BindableProperty.Create(
        nameof(ServiceRows), typeof(IEnumerable), typeof(ServicesListSectionView));

    public static readonly BindableProperty ServicesListHeightProperty = BindableProperty.Create(
        nameof(ServicesListHeight), typeof(double), typeof(ServicesListSectionView), 0d);

    public bool HasServices
    {
        get => (bool)GetValue(HasServicesProperty);
        set => SetValue(HasServicesProperty, value);
    }

    public string ServicesCountText
    {
        get => (string)GetValue(ServicesCountTextProperty);
        set => SetValue(ServicesCountTextProperty, value);
    }

    public IEnumerable? ServiceRows
    {
        get => (IEnumerable?)GetValue(ServiceRowsProperty);
        set => SetValue(ServiceRowsProperty, value);
    }

    public double ServicesListHeight
    {
        get => (double)GetValue(ServicesListHeightProperty);
        set => SetValue(ServicesListHeightProperty, value);
    }

    public ServicesListSectionView()
    {
        InitializeComponent();
    }
}
