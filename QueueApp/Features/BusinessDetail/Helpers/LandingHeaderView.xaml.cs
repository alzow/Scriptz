using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class LandingHeaderView : ContentView
{
    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(LandingHeaderView), string.Empty);

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(LandingHeaderView), false);

    public static readonly BindableProperty OpenPillTextProperty = BindableProperty.Create(
        nameof(OpenPillText), typeof(string), typeof(LandingHeaderView), string.Empty);

    public static readonly BindableProperty AddressLineProperty = BindableProperty.Create(
        nameof(AddressLine), typeof(string), typeof(LandingHeaderView), string.Empty);

    public static readonly BindableProperty ModeLineProperty = BindableProperty.Create(
        nameof(ModeLine), typeof(string), typeof(LandingHeaderView), string.Empty);

    public static readonly BindableProperty GoBackCommandProperty = BindableProperty.Create(
        nameof(GoBackCommand), typeof(ICommand), typeof(LandingHeaderView));

    public string BusinessName
    {
        get => (string)GetValue(BusinessNameProperty);
        set => SetValue(BusinessNameProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string OpenPillText
    {
        get => (string)GetValue(OpenPillTextProperty);
        set => SetValue(OpenPillTextProperty, value);
    }

    public string AddressLine
    {
        get => (string)GetValue(AddressLineProperty);
        set => SetValue(AddressLineProperty, value);
    }

    public string ModeLine
    {
        get => (string)GetValue(ModeLineProperty);
        set => SetValue(ModeLineProperty, value);
    }

    public ICommand? GoBackCommand
    {
        get => (ICommand?)GetValue(GoBackCommandProperty);
        set => SetValue(GoBackCommandProperty, value);
    }

    public LandingHeaderView()
    {
        InitializeComponent();
    }
}
