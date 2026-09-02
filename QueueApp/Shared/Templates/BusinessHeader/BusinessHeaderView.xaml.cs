using System.Windows.Input;

namespace QueueApp.Shared.Templates.BusinessHeader;

/// <summary>
/// The header worn by every page that sits on a single business. <see cref="PillTone"/> takes
/// "Good", "Muted" or "Bad" — open/closed on a landing, live/settled/gone wrong on a visit — and
/// the trailing action is opt-in, so a page that has nothing to offer there just leaves it off.
/// </summary>
public partial class BusinessHeaderView : ContentView
{
    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty AddressLineProperty = BindableProperty.Create(
        nameof(AddressLine), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty MetaLineProperty = BindableProperty.Create(
        nameof(MetaLine), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty PillTextProperty = BindableProperty.Create(
        nameof(PillText), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty PillToneProperty = BindableProperty.Create(
        nameof(PillTone), typeof(string), typeof(BusinessHeaderView), "Good");

    public static readonly BindableProperty GoBackCommandProperty = BindableProperty.Create(
        nameof(GoBackCommand), typeof(ICommand), typeof(BusinessHeaderView));

    public static readonly BindableProperty HasActionProperty = BindableProperty.Create(
        nameof(HasAction), typeof(bool), typeof(BusinessHeaderView), false);

    public static readonly BindableProperty ActionIconProperty = BindableProperty.Create(
        nameof(ActionIcon), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(BusinessHeaderView), string.Empty);

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand), typeof(ICommand), typeof(BusinessHeaderView));

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

    public string MetaLine
    {
        get => (string)GetValue(MetaLineProperty);
        set => SetValue(MetaLineProperty, value);
    }

    public string PillText
    {
        get => (string)GetValue(PillTextProperty);
        set => SetValue(PillTextProperty, value);
    }

    public string PillTone
    {
        get => (string)GetValue(PillToneProperty);
        set => SetValue(PillToneProperty, value);
    }

    public ICommand? GoBackCommand
    {
        get => (ICommand?)GetValue(GoBackCommandProperty);
        set => SetValue(GoBackCommandProperty, value);
    }

    public bool HasAction
    {
        get => (bool)GetValue(HasActionProperty);
        set => SetValue(HasActionProperty, value);
    }

    public string ActionIcon
    {
        get => (string)GetValue(ActionIconProperty);
        set => SetValue(ActionIconProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public BusinessHeaderView()
    {
        InitializeComponent();
    }
}
