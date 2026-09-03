using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeSingleSelectFieldView : ContentView
{
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options), typeof(IEnumerable), typeof(IntakeSingleSelectFieldView));

    public static readonly BindableProperty SelectOptionCommandProperty = BindableProperty.Create(
        nameof(SelectOptionCommand), typeof(ICommand), typeof(IntakeSingleSelectFieldView));

    public IEnumerable? Options
    {
        get => (IEnumerable?)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public ICommand? SelectOptionCommand
    {
        get => (ICommand?)GetValue(SelectOptionCommandProperty);
        set => SetValue(SelectOptionCommandProperty, value);
    }

    public IntakeSingleSelectFieldView()
    {
        InitializeComponent();
    }
}
