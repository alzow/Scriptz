using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class AgendaHeaderView : ContentView
{
    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(AgendaHeaderView), string.Empty);

    public static readonly BindableProperty IsOpenNowProperty = BindableProperty.Create(
        nameof(IsOpenNow), typeof(bool), typeof(AgendaHeaderView), false);

    public static readonly BindableProperty OpenLabelProperty = BindableProperty.Create(
        nameof(OpenLabel), typeof(string), typeof(AgendaHeaderView), string.Empty);

    public static readonly BindableProperty OpenSettingsCommandProperty = BindableProperty.Create(
        nameof(OpenSettingsCommand), typeof(ICommand), typeof(AgendaHeaderView));

    public string BusinessName
    {
        get => (string)GetValue(BusinessNameProperty);
        set => SetValue(BusinessNameProperty, value);
    }

    public bool IsOpenNow
    {
        get => (bool)GetValue(IsOpenNowProperty);
        set => SetValue(IsOpenNowProperty, value);
    }

    public string OpenLabel
    {
        get => (string)GetValue(OpenLabelProperty);
        set => SetValue(OpenLabelProperty, value);
    }

    public ICommand? OpenSettingsCommand
    {
        get => (ICommand?)GetValue(OpenSettingsCommandProperty);
        set => SetValue(OpenSettingsCommandProperty, value);
    }

    public AgendaHeaderView()
    {
        InitializeComponent();
    }
}
