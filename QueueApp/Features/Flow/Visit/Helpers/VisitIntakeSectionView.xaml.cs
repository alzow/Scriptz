using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitIntakeSectionView : ContentView
{
    public static readonly BindableProperty SectionTitleProperty = BindableProperty.Create(
        nameof(SectionTitle), typeof(string), typeof(VisitIntakeSectionView), string.Empty);

    public static readonly BindableProperty AnswersProperty = BindableProperty.Create(
        nameof(Answers), typeof(IEnumerable), typeof(VisitIntakeSectionView));

    public static readonly BindableProperty OpenFileCommandProperty = BindableProperty.Create(
        nameof(OpenFileCommand), typeof(ICommand), typeof(VisitIntakeSectionView));

    public string SectionTitle
    {
        get => (string)GetValue(SectionTitleProperty);
        set => SetValue(SectionTitleProperty, value);
    }

    public IEnumerable? Answers
    {
        get => (IEnumerable?)GetValue(AnswersProperty);
        set => SetValue(AnswersProperty, value);
    }

    public ICommand? OpenFileCommand
    {
        get => (ICommand?)GetValue(OpenFileCommandProperty);
        set => SetValue(OpenFileCommandProperty, value);
    }

    public VisitIntakeSectionView()
    {
        InitializeComponent();
    }
}
