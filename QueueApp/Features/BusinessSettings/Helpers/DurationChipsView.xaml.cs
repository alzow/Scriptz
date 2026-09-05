using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessSettings.Helpers;

public partial class DurationChipsView : ContentView
{
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options), typeof(IEnumerable), typeof(DurationChipsView));

    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(DurationChipsView));

    public IEnumerable? Options
    {
        get => (IEnumerable?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public DurationChipsView()
    {
        InitializeComponent();
    }
}
