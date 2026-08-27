using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class GettingThereSectionView : ContentView
{
    public static readonly BindableProperty AddressLineProperty = BindableProperty.Create(
        nameof(AddressLine), typeof(string), typeof(GettingThereSectionView), string.Empty);

    public static readonly BindableProperty DistanceTextProperty = BindableProperty.Create(
        nameof(DistanceText), typeof(string), typeof(GettingThereSectionView), string.Empty);

    public static readonly BindableProperty HasDistanceProperty = BindableProperty.Create(
        nameof(HasDistance), typeof(bool), typeof(GettingThereSectionView), false);

    public static readonly BindableProperty OpenDirectionsCommandProperty = BindableProperty.Create(
        nameof(OpenDirectionsCommand), typeof(ICommand), typeof(GettingThereSectionView));

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

    public GettingThereSectionView()
    {
        InitializeComponent();
    }
}
