using System.Windows.Input;

namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class AwaitingCollectionSectionView : ContentView
{
    public static readonly BindableProperty HasAwaitingCollectionProperty = BindableProperty.Create(
        nameof(HasAwaitingCollection), typeof(bool), typeof(AwaitingCollectionSectionView), false);

    public static readonly BindableProperty AwaitingCollectionRowsProperty = BindableProperty.Create(
        nameof(AwaitingCollectionRows), typeof(object), typeof(AwaitingCollectionSectionView));

    public static readonly BindableProperty MarkCollectedCommandProperty = BindableProperty.Create(
        nameof(MarkCollectedCommand), typeof(ICommand), typeof(AwaitingCollectionSectionView));

    public bool HasAwaitingCollection
    {
        get => (bool)GetValue(HasAwaitingCollectionProperty);
        set => SetValue(HasAwaitingCollectionProperty, value);
    }

    public object? AwaitingCollectionRows
    {
        get => GetValue(AwaitingCollectionRowsProperty);
        set => SetValue(AwaitingCollectionRowsProperty, value);
    }

    public ICommand? MarkCollectedCommand
    {
        get => (ICommand?)GetValue(MarkCollectedCommandProperty);
        set => SetValue(MarkCollectedCommandProperty, value);
    }

    public AwaitingCollectionSectionView()
    {
        InitializeComponent();
    }
}
