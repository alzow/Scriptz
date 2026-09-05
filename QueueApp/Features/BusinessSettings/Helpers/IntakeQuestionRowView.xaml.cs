using System.Windows.Input;

namespace QueueApp.Features.BusinessSettings.Helpers;

public partial class IntakeQuestionRowView : ContentView
{
    public static readonly BindableProperty PromptProperty = BindableProperty.Create(
        nameof(Prompt), typeof(string), typeof(IntakeQuestionRowView), string.Empty);

    public static readonly BindableProperty SummaryTextProperty = BindableProperty.Create(
        nameof(SummaryText), typeof(string), typeof(IntakeQuestionRowView), string.Empty);

    public static readonly BindableProperty ConditionTextProperty = BindableProperty.Create(
        nameof(ConditionText), typeof(string), typeof(IntakeQuestionRowView), string.Empty);

    public static readonly BindableProperty HasConditionProperty = BindableProperty.Create(
        nameof(HasCondition), typeof(bool), typeof(IntakeQuestionRowView), false);

    public static readonly BindableProperty EditCommandProperty = BindableProperty.Create(
        nameof(EditCommand), typeof(ICommand), typeof(IntakeQuestionRowView));

    public static readonly BindableProperty MoveUpCommandProperty = BindableProperty.Create(
        nameof(MoveUpCommand), typeof(ICommand), typeof(IntakeQuestionRowView));

    public static readonly BindableProperty MoveDownCommandProperty = BindableProperty.Create(
        nameof(MoveDownCommand), typeof(ICommand), typeof(IntakeQuestionRowView));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(IntakeQuestionRowView));

    public string Prompt
    {
        get => (string)GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }

    public string SummaryText
    {
        get => (string)GetValue(SummaryTextProperty);
        set => SetValue(SummaryTextProperty, value);
    }

    public string ConditionText
    {
        get => (string)GetValue(ConditionTextProperty);
        set => SetValue(ConditionTextProperty, value);
    }

    public bool HasCondition
    {
        get => (bool)GetValue(HasConditionProperty);
        set => SetValue(HasConditionProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? MoveUpCommand
    {
        get => (ICommand?)GetValue(MoveUpCommandProperty);
        set => SetValue(MoveUpCommandProperty, value);
    }

    public ICommand? MoveDownCommand
    {
        get => (ICommand?)GetValue(MoveDownCommandProperty);
        set => SetValue(MoveDownCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IntakeQuestionRowView()
    {
        InitializeComponent();
    }
}
