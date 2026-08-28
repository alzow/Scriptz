using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers;

public partial class OperatorStepView : ContentView
{
    public static readonly BindableProperty OperatorChoicesProperty = BindableProperty.Create(
        nameof(OperatorChoices), typeof(IEnumerable), typeof(OperatorStepView));

    public static readonly BindableProperty ShowOperatorStepProperty = BindableProperty.Create(
        nameof(ShowOperatorStep), typeof(bool), typeof(OperatorStepView), false);

    public static readonly BindableProperty SelectOperatorCommandProperty = BindableProperty.Create(
        nameof(SelectOperatorCommand), typeof(ICommand), typeof(OperatorStepView));

    public IEnumerable? OperatorChoices
    {
        get => (IEnumerable?)GetValue(OperatorChoicesProperty);
        set => SetValue(OperatorChoicesProperty, value);
    }

    public bool ShowOperatorStep
    {
        get => (bool)GetValue(ShowOperatorStepProperty);
        set => SetValue(ShowOperatorStepProperty, value);
    }

    public ICommand? SelectOperatorCommand
    {
        get => (ICommand?)GetValue(SelectOperatorCommandProperty);
        set => SetValue(SelectOperatorCommandProperty, value);
    }

    public OperatorStepView()
    {
        InitializeComponent();
    }
}
