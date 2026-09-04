using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class PoolBannerView : ContentView
{
    public static readonly BindableProperty PoolStrokeProperty = BindableProperty.Create(
        nameof(PoolStroke), typeof(Brush), typeof(PoolBannerView));

    public static readonly BindableProperty PoolStrokeThicknessProperty = BindableProperty.Create(
        nameof(PoolStrokeThickness), typeof(double), typeof(PoolBannerView), 1d);

    public static readonly BindableProperty HasPoolProperty = BindableProperty.Create(
        nameof(HasPool), typeof(bool), typeof(PoolBannerView), false);

    public static readonly BindableProperty PoolCountTextProperty = BindableProperty.Create(
        nameof(PoolCountText), typeof(string), typeof(PoolBannerView), string.Empty);

    public static readonly BindableProperty PoolAgeTextProperty = BindableProperty.Create(
        nameof(PoolAgeText), typeof(string), typeof(PoolBannerView), string.Empty);

    public static readonly BindableProperty PoolChevronProperty = BindableProperty.Create(
        nameof(PoolChevron), typeof(string), typeof(PoolBannerView), string.Empty);

    public static readonly BindableProperty IsPoolExpandedProperty = BindableProperty.Create(
        nameof(IsPoolExpanded), typeof(bool), typeof(PoolBannerView), false);

    public static readonly BindableProperty PoolRowsProperty = BindableProperty.Create(
        nameof(PoolRows), typeof(IEnumerable), typeof(PoolBannerView));

    public static readonly BindableProperty TogglePoolCommandProperty = BindableProperty.Create(
        nameof(TogglePoolCommand), typeof(ICommand), typeof(PoolBannerView));

    public static readonly BindableProperty OpenRowSheetCommandProperty = BindableProperty.Create(
        nameof(OpenRowSheetCommand), typeof(ICommand), typeof(PoolBannerView));

    public Brush? PoolStroke
    {
        get => (Brush?)GetValue(PoolStrokeProperty);
        set => SetValue(PoolStrokeProperty, value);
    }

    public double PoolStrokeThickness
    {
        get => (double)GetValue(PoolStrokeThicknessProperty);
        set => SetValue(PoolStrokeThicknessProperty, value);
    }

    public bool HasPool
    {
        get => (bool)GetValue(HasPoolProperty);
        set => SetValue(HasPoolProperty, value);
    }

    public string PoolCountText
    {
        get => (string)GetValue(PoolCountTextProperty);
        set => SetValue(PoolCountTextProperty, value);
    }

    public string PoolAgeText
    {
        get => (string)GetValue(PoolAgeTextProperty);
        set => SetValue(PoolAgeTextProperty, value);
    }

    public string PoolChevron
    {
        get => (string)GetValue(PoolChevronProperty);
        set => SetValue(PoolChevronProperty, value);
    }

    public bool IsPoolExpanded
    {
        get => (bool)GetValue(IsPoolExpandedProperty);
        set => SetValue(IsPoolExpandedProperty, value);
    }

    public IEnumerable? PoolRows
    {
        get => (IEnumerable?)GetValue(PoolRowsProperty);
        set => SetValue(PoolRowsProperty, value);
    }

    public ICommand? TogglePoolCommand
    {
        get => (ICommand?)GetValue(TogglePoolCommandProperty);
        set => SetValue(TogglePoolCommandProperty, value);
    }

    public ICommand? OpenRowSheetCommand
    {
        get => (ICommand?)GetValue(OpenRowSheetCommandProperty);
        set => SetValue(OpenRowSheetCommandProperty, value);
    }

    public PoolBannerView()
    {
        InitializeComponent();
    }
}
