using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeMultiSelectFieldView : ContentView
{
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options), typeof(IEnumerable), typeof(IntakeMultiSelectFieldView));

    public static readonly BindableProperty ToggleOptionCommandProperty = BindableProperty.Create(
        nameof(ToggleOptionCommand), typeof(ICommand), typeof(IntakeMultiSelectFieldView));

    public IEnumerable? Options
    {
        get => (IEnumerable?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public ICommand? ToggleOptionCommand
    {
        get => (ICommand?)GetValue(ToggleOptionCommandProperty);
        set => SetValue(ToggleOptionCommandProperty, value);
    }

    public IntakeMultiSelectFieldView()
    {
        InitializeComponent();
    }
}
