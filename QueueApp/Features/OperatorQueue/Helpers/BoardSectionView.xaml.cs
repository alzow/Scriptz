using System.Windows.Input;

namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class BoardSectionView : ContentView
{
    public static readonly BindableProperty EditNoteCommandProperty = BindableProperty.Create(
        nameof(EditNoteCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty DoneCommandProperty = BindableProperty.Create(
        nameof(DoneCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty ServeCommandProperty = BindableProperty.Create(
        nameof(ServeCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty OpenRowActionsCommandProperty = BindableProperty.Create(
        nameof(OpenRowActionsCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty ViewAnswersCommandProperty = BindableProperty.Create(
        nameof(ViewAnswersCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty AddWalkInCommandProperty = BindableProperty.Create(
        nameof(AddWalkInCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty ToggleShiftCommandProperty = BindableProperty.Create(
        nameof(ToggleShiftCommand), typeof(ICommand), typeof(BoardSectionView));

    public ICommand? EditNoteCommand
    {
        get => (ICommand?)GetValue(EditNoteCommandProperty);
        set => SetValue(EditNoteCommandProperty, value);
    }

    public ICommand? DoneCommand
    {
        get => (ICommand?)GetValue(DoneCommandProperty);
        set => SetValue(DoneCommandProperty, value);
    }

    public ICommand? ServeCommand
    {
        get => (ICommand?)GetValue(ServeCommandProperty);
        set => SetValue(ServeCommandProperty, value);
    }

    public ICommand? OpenRowActionsCommand
    {
        get => (ICommand?)GetValue(OpenRowActionsCommandProperty);
        set => SetValue(OpenRowActionsCommandProperty, value);
    }

    public ICommand? ViewAnswersCommand
    {
        get => (ICommand?)GetValue(ViewAnswersCommandProperty);
        set => SetValue(ViewAnswersCommandProperty, value);
    }

    public ICommand? AddWalkInCommand
    {
        get => (ICommand?)GetValue(AddWalkInCommandProperty);
        set => SetValue(AddWalkInCommandProperty, value);
    }

    public ICommand? ToggleShiftCommand
    {
        get => (ICommand?)GetValue(ToggleShiftCommandProperty);
        set => SetValue(ToggleShiftCommandProperty, value);
    }

    public BoardSectionView()
    {
        InitializeComponent();
    }
}
