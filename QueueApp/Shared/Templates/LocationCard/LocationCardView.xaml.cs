using System.Windows.Input;

namespace QueueApp.Shared.Templates.LocationCard;

/// <summary>
/// The "Getting there" card: the shop's address, how far off it is, and a tap that hands the
/// coordinates to the phone's map. Shared by the business landing and the visit page so a shop's
/// address is presented the same way wherever it is met.
/// </summary>
public partial class LocationCardView : ContentView
{
    public static readonly BindableProperty SectionTitleProperty = BindableProperty.Create(
        nameof(SectionTitle), typeof(string), typeof(LocationCardView), "Getting there");

    public static readonly BindableProperty AddressLineProperty = BindableProperty.Create(
        nameof(AddressLine), typeof(string), typeof(LocationCardView), string.Empty);

    public static readonly BindableProperty DistanceTextProperty = BindableProperty.Create(
        nameof(DistanceText), typeof(string), typeof(LocationCardView), string.Empty);

    public static readonly BindableProperty HasDistanceProperty = BindableProperty.Create(
        nameof(HasDistance), typeof(bool), typeof(LocationCardView), false);

    public static readonly BindableProperty OpenDirectionsCommandProperty = BindableProperty.Create(
        nameof(OpenDirectionsCommand), typeof(ICommand), typeof(LocationCardView));

    public string SectionTitle
    {
        get => (string)GetValue(SectionTitleProperty);
        set => SetValue(SectionTitleProperty, value);
    }

    public string AddressLine
    {
        get => (string)GetValue(AddressLineProperty);
        set => SetValue(AddressLineProperty, value);
    }

    public string DistanceText
    {
        get => (string)GetValue(DistanceTextProperty);
        set => SetValue(DistanceTextProperty, value);
    }

    public bool HasDistance
    {
        get => (bool)GetValue(HasDistanceProperty);
        set => SetValue(HasDistanceProperty, value);
    }

    public ICommand? OpenDirectionsCommand
    {
        get => (ICommand?)GetValue(OpenDirectionsCommandProperty);
        set => SetValue(OpenDirectionsCommandProperty, value);
    }

    public LocationCardView()
    {
        InitializeComponent();
    }
}
