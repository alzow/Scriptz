using System.Windows.Input;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitHeaderView : ContentView
{
    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(VisitHeaderView), string.Empty);

    public static readonly BindableProperty AddressLineProperty = BindableProperty.Create(
        nameof(AddressLine), typeof(string), typeof(VisitHeaderView), string.Empty);

    public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
        nameof(StatusText), typeof(string), typeof(VisitHeaderView), string.Empty);

    public static readonly BindableProperty StatusToneProperty = BindableProperty.Create(
        nameof(StatusTone), typeof(string), typeof(VisitHeaderView), "Live");

    public static readonly BindableProperty HasPhoneProperty = BindableProperty.Create(
        nameof(HasPhone), typeof(bool), typeof(VisitHeaderView), false);

    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(
        nameof(BackCommand), typeof(ICommand), typeof(VisitHeaderView));

    public static readonly BindableProperty CallCommandProperty = BindableProperty.Create(
        nameof(CallCommand), typeof(ICommand), typeof(VisitHeaderView));

    public string BusinessName
    {
        get => (string)GetValue(BusinessNameProperty);
        set => SetValue(BusinessNameProperty, value);
    }

    public string AddressLine
    {
        get => (string)GetValue(AddressLineProperty);
        set => SetValue(AddressLineProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public string StatusTone
    {
        get => (string)GetValue(StatusToneProperty);
        set => SetValue(StatusToneProperty, value);
    }

    public bool HasPhone
    {
        get => (bool)GetValue(HasPhoneProperty);
        set => SetValue(HasPhoneProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public ICommand? CallCommand
    {
        get => (ICommand?)GetValue(CallCommandProperty);
        set => SetValue(CallCommandProperty, value);
    }

    public VisitHeaderView()
    {
        InitializeComponent();
    }
}
