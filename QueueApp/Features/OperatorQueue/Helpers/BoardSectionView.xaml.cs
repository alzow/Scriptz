using System.Windows.Input;

namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class BoardSectionView : ContentView
{
    public static readonly BindableProperty OpenRowSheetCommandProperty = BindableProperty.Create(
        nameof(OpenRowSheetCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty OpenServingSheetCommandProperty = BindableProperty.Create(
        nameof(OpenServingSheetCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty AddToQueueCommandProperty = BindableProperty.Create(
        nameof(AddToQueueCommand), typeof(ICommand), typeof(BoardSectionView));

    public static readonly BindableProperty ToggleShiftCommandProperty = BindableProperty.Create(
        nameof(ToggleShiftCommand), typeof(ICommand), typeof(BoardSectionView));

    public ICommand? OpenRowSheetCommand
    {
        get => (ICommand?)GetValue(OpenRowSheetCommandProperty);
        set => SetValue(OpenRowSheetCommandProperty, value);
    }

    public ICommand? OpenServingSheetCommand
    {
        get => (ICommand?)GetValue(OpenServingSheetCommandProperty);
        set => SetValue(OpenServingSheetCommandProperty, value);
    }

    public ICommand? AddToQueueCommand
    {
        get => (ICommand?)GetValue(AddToQueueCommandProperty);
        set => SetValue(AddToQueueCommandProperty, value);
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
