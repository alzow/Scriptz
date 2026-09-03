using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeStepView : ContentView
{
    public static readonly BindableProperty IntakeFieldsProperty = BindableProperty.Create(
        nameof(IntakeFields), typeof(IEnumerable), typeof(IntakeStepView));

    public static readonly BindableProperty ShowIntakeStepProperty = BindableProperty.Create(
        nameof(ShowIntakeStep), typeof(bool), typeof(IntakeStepView), false);

    // One command for both select types: which of them a tap means is the field's business, not
    // the step's.
    public static readonly BindableProperty SelectOptionCommandProperty = BindableProperty.Create(
        nameof(SelectOptionCommand), typeof(ICommand), typeof(IntakeStepView));

    public static readonly BindableProperty PickFileCommandProperty = BindableProperty.Create(
        nameof(PickFileCommand), typeof(ICommand), typeof(IntakeStepView));

    public static readonly BindableProperty ClearFileCommandProperty = BindableProperty.Create(
        nameof(ClearFileCommand), typeof(ICommand), typeof(IntakeStepView));

    public IEnumerable? IntakeFields
    {
        get => (IEnumerable?)GetValue(IntakeFieldsProperty);
        set => SetValue(IntakeFieldsProperty, value);
    }

    public bool ShowIntakeStep
    {
        get => (bool)GetValue(ShowIntakeStepProperty);
        set => SetValue(ShowIntakeStepProperty, value);
    }

    public ICommand? SelectOptionCommand
    {
        get => (ICommand?)GetValue(SelectOptionCommandProperty);
        set => SetValue(SelectOptionCommandProperty, value);
    }

    public ICommand? PickFileCommand
    {
        get => (ICommand?)GetValue(PickFileCommandProperty);
        set => SetValue(PickFileCommandProperty, value);
    }

    public ICommand? ClearFileCommand
    {
        get => (ICommand?)GetValue(ClearFileCommandProperty);
        set => SetValue(ClearFileCommandProperty, value);
    }

    public IntakeStepView()
    {
        InitializeComponent();
    }
}
