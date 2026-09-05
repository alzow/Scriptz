using System.Windows.Input;

namespace QueueApp.Features.BusinessSettings.Helpers;

public partial class ServiceRowView : ContentView
{
    public static readonly BindableProperty ServiceNameProperty = BindableProperty.Create(
        nameof(ServiceName), typeof(string), typeof(ServiceRowView), string.Empty);

    public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
        nameof(DetailText), typeof(string), typeof(ServiceRowView), string.Empty);

    public static readonly BindableProperty HasQuestionsProperty = BindableProperty.Create(
        nameof(HasQuestions), typeof(bool), typeof(ServiceRowView), false);

    public static readonly BindableProperty QuestionChipTextProperty = BindableProperty.Create(
        nameof(QuestionChipText), typeof(string), typeof(ServiceRowView), string.Empty);

    public static readonly BindableProperty RequiresCollectionProperty = BindableProperty.Create(
        nameof(RequiresCollection), typeof(bool), typeof(ServiceRowView), false);

    public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
        nameof(TapCommand), typeof(ICommand), typeof(ServiceRowView));

    public static readonly BindableProperty TapCommandParameterProperty = BindableProperty.Create(
        nameof(TapCommandParameter), typeof(object), typeof(ServiceRowView));

    public string ServiceName
    {
        get => (string)GetValue(ServiceNameProperty);
        set => SetValue(ServiceNameProperty, value);
    }

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    public bool HasQuestions
    {
        get => (bool)GetValue(HasQuestionsProperty);
        set => SetValue(HasQuestionsProperty, value);
    }

    public string QuestionChipText
    {
        get => (string)GetValue(QuestionChipTextProperty);
        set => SetValue(QuestionChipTextProperty, value);
    }

    public bool RequiresCollection
    {
        get => (bool)GetValue(RequiresCollectionProperty);
        set => SetValue(RequiresCollectionProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public object? TapCommandParameter
    {
        get => GetValue(TapCommandParameterProperty);
        set => SetValue(TapCommandParameterProperty, value);
    }

    public ServiceRowView()
    {
        InitializeComponent();
    }
}
