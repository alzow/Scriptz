using System.Collections;

namespace QueueApp.Features.Welcome.Helpers;

public partial class WelcomeDotsView : ContentView
{
    public static readonly BindableProperty PanelsProperty = BindableProperty.Create(
        nameof(Panels), typeof(IEnumerable), typeof(WelcomeDotsView));

    public IEnumerable? Panels
    {
        get => (IEnumerable?)GetValue(PanelsProperty);
        set => SetValue(PanelsProperty, value);
    }

    public WelcomeDotsView()
    {
        InitializeComponent();
    }
}
