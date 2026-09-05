using System.Windows.Input;

namespace QueueApp.Features.BusinessSettings.Helpers;

public partial class IntakeRuleBuilderView : ContentView
{
    public static readonly BindableProperty QuestionTextProperty = BindableProperty.Create(
        nameof(QuestionText), typeof(string), typeof(IntakeRuleBuilderView), string.Empty);

    public static readonly BindableProperty ValueTextProperty = BindableProperty.Create(
        nameof(ValueText), typeof(string), typeof(IntakeRuleBuilderView), string.Empty);

    public static readonly BindableProperty PickQuestionCommandProperty = BindableProperty.Create(
        nameof(PickQuestionCommand), typeof(ICommand), typeof(IntakeRuleBuilderView));

    public static readonly BindableProperty PickValueCommandProperty = BindableProperty.Create(
        nameof(PickValueCommand), typeof(ICommand), typeof(IntakeRuleBuilderView));

    public string QuestionText
    {
        get => (string)GetValue(QuestionTextProperty);
        set => SetValue(QuestionTextProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public ICommand? PickQuestionCommand
    {
        get => (ICommand?)GetValue(PickQuestionCommandProperty);
        set => SetValue(PickQuestionCommandProperty, value);
    }

    public ICommand? PickValueCommand
    {
        get => (ICommand?)GetValue(PickValueCommandProperty);
        set => SetValue(PickValueCommandProperty, value);
    }

    public IntakeRuleBuilderView()
    {
        InitializeComponent();
    }
}
