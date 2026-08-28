using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class RequestsBannerView : ContentView
{
    public static readonly BindableProperty HasRequestsProperty = BindableProperty.Create(
        nameof(HasRequests), typeof(bool), typeof(RequestsBannerView), false);

    public static readonly BindableProperty IsRequestsExpandedProperty = BindableProperty.Create(
        nameof(IsRequestsExpanded), typeof(bool), typeof(RequestsBannerView), false);

    public static readonly BindableProperty RequestsCountTextProperty = BindableProperty.Create(
        nameof(RequestsCountText), typeof(string), typeof(RequestsBannerView), string.Empty);

    public static readonly BindableProperty RequestsAgeTextProperty = BindableProperty.Create(
        nameof(RequestsAgeText), typeof(string), typeof(RequestsBannerView), string.Empty);

    public static readonly BindableProperty RequestsChevronProperty = BindableProperty.Create(
        nameof(RequestsChevron), typeof(string), typeof(RequestsBannerView), string.Empty);

    public static readonly BindableProperty RequestsStrokeProperty = BindableProperty.Create(
        nameof(RequestsStroke), typeof(Brush), typeof(RequestsBannerView));

    public static readonly BindableProperty RequestsStrokeThicknessProperty = BindableProperty.Create(
        nameof(RequestsStrokeThickness), typeof(double), typeof(RequestsBannerView), 1d);

    public static readonly BindableProperty RequestsProperty = BindableProperty.Create(
        nameof(Requests), typeof(IEnumerable), typeof(RequestsBannerView));

    public static readonly BindableProperty ToggleRequestsCommandProperty = BindableProperty.Create(
        nameof(ToggleRequestsCommand), typeof(ICommand), typeof(RequestsBannerView));

    public static readonly BindableProperty ConfirmRequestCommandProperty = BindableProperty.Create(
        nameof(ConfirmRequestCommand), typeof(ICommand), typeof(RequestsBannerView));

    public static readonly BindableProperty DeclineRequestCommandProperty = BindableProperty.Create(
        nameof(DeclineRequestCommand), typeof(ICommand), typeof(RequestsBannerView));

    public bool HasRequests
    {
        get => (bool)GetValue(HasRequestsProperty);
        set => SetValue(HasRequestsProperty, value);
    }

    public bool IsRequestsExpanded
    {
        get => (bool)GetValue(IsRequestsExpandedProperty);
        set => SetValue(IsRequestsExpandedProperty, value);
    }

    public string? RequestsCountText
    {
        get => (string?)GetValue(RequestsCountTextProperty);
        set => SetValue(RequestsCountTextProperty, value);
    }

    public string? RequestsAgeText
    {
        get => (string?)GetValue(RequestsAgeTextProperty);
        set => SetValue(RequestsAgeTextProperty, value);
    }

    public string? RequestsChevron
    {
        get => (string?)GetValue(RequestsChevronProperty);
        set => SetValue(RequestsChevronProperty, value);
    }

    public Brush? RequestsStroke
    {
        get => (Brush?)GetValue(RequestsStrokeProperty);
        set => SetValue(RequestsStrokeProperty, value);
    }

    public double RequestsStrokeThickness
    {
        get => (double)GetValue(RequestsStrokeThicknessProperty);
        set => SetValue(RequestsStrokeThicknessProperty, value);
    }

    public IEnumerable? Requests
    {
        get => (IEnumerable?)GetValue(RequestsProperty);
        set => SetValue(RequestsProperty, value);
    }

    public ICommand? ToggleRequestsCommand
    {
        get => (ICommand?)GetValue(ToggleRequestsCommandProperty);
        set => SetValue(ToggleRequestsCommandProperty, value);
    }

    public ICommand? ConfirmRequestCommand
    {
        get => (ICommand?)GetValue(ConfirmRequestCommandProperty);
        set => SetValue(ConfirmRequestCommandProperty, value);
    }

    public ICommand? DeclineRequestCommand
    {
        get => (ICommand?)GetValue(DeclineRequestCommandProperty);
        set => SetValue(DeclineRequestCommandProperty, value);
    }

    public RequestsBannerView()
    {
        InitializeComponent();
    }
}
