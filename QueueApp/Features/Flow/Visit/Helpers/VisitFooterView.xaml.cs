using System.Windows.Input;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitFooterView : ContentView
{
    public static readonly BindableProperty PrimaryActionTextProperty = BindableProperty.Create(
        nameof(PrimaryActionText), typeof(string), typeof(VisitFooterView), string.Empty);

    public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
        nameof(PrimaryActionCommand), typeof(ICommand), typeof(VisitFooterView));

    public static readonly BindableProperty CanAddToCalendarProperty = BindableProperty.Create(
        nameof(CanAddToCalendar), typeof(bool), typeof(VisitFooterView), false);

    public static readonly BindableProperty AddToCalendarCommandProperty = BindableProperty.Create(
        nameof(AddToCalendarCommand), typeof(ICommand), typeof(VisitFooterView));

    public static readonly BindableProperty HasDestructiveActionProperty = BindableProperty.Create(
        nameof(HasDestructiveAction), typeof(bool), typeof(VisitFooterView), false);

    public static readonly BindableProperty DestructiveActionTextProperty = BindableProperty.Create(
        nameof(DestructiveActionText), typeof(string), typeof(VisitFooterView), string.Empty);

    public static readonly BindableProperty DestructiveActionCommandProperty = BindableProperty.Create(
        nameof(DestructiveActionCommand), typeof(ICommand), typeof(VisitFooterView));

    public static readonly BindableProperty ShowPaymentLineProperty = BindableProperty.Create(
        nameof(ShowPaymentLine), typeof(bool), typeof(VisitFooterView), false);

    public string PrimaryActionText
    {
        get => (string)GetValue(PrimaryActionTextProperty);
        set => SetValue(PrimaryActionTextProperty, value);
    }

    public ICommand? PrimaryActionCommand
    {
        get => (ICommand?)GetValue(PrimaryActionCommandProperty);
        set => SetValue(PrimaryActionCommandProperty, value);
    }

    public bool CanAddToCalendar
    {
        get => (bool)GetValue(CanAddToCalendarProperty);
        set => SetValue(CanAddToCalendarProperty, value);
    }

    public ICommand? AddToCalendarCommand
    {
        get => (ICommand?)GetValue(AddToCalendarCommandProperty);
        set => SetValue(AddToCalendarCommandProperty, value);
    }

    public bool HasDestructiveAction
    {
        get => (bool)GetValue(HasDestructiveActionProperty);
        set => SetValue(HasDestructiveActionProperty, value);
    }

    public string DestructiveActionText
    {
        get => (string)GetValue(DestructiveActionTextProperty);
        set => SetValue(DestructiveActionTextProperty, value);
    }

    public ICommand? DestructiveActionCommand
    {
        get => (ICommand?)GetValue(DestructiveActionCommandProperty);
        set => SetValue(DestructiveActionCommandProperty, value);
    }

    public bool ShowPaymentLine
    {
        get => (bool)GetValue(ShowPaymentLineProperty);
        set => SetValue(ShowPaymentLineProperty, value);
    }

    public VisitFooterView()
    {
        InitializeComponent();
    }
}
