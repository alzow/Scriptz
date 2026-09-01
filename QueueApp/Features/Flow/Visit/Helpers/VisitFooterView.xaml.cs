using System.Windows.Input;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitFooterView : ContentView
{
    public static readonly BindableProperty PrimaryActionTextProperty = BindableProperty.Create(
        nameof(PrimaryActionText), typeof(string), typeof(VisitFooterView), string.Empty);

    public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
        nameof(PrimaryActionCommand), typeof(ICommand), typeof(VisitFooterView));

    public static readonly BindableProperty OptionsCommandProperty = BindableProperty.Create(
        nameof(OptionsCommand), typeof(ICommand), typeof(VisitFooterView));

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

    public ICommand? OptionsCommand
    {
        get => (ICommand?)GetValue(OptionsCommandProperty);
        set => SetValue(OptionsCommandProperty, value);
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
