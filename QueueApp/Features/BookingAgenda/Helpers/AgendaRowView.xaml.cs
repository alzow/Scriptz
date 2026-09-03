using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class AgendaRowView : ContentView
{
    public static readonly BindableProperty RowTappedCommandProperty = BindableProperty.Create(
        nameof(RowTappedCommand), typeof(ICommand), typeof(AgendaRowView));

    public static readonly BindableProperty FillGapCommandProperty = BindableProperty.Create(
        nameof(FillGapCommand), typeof(ICommand), typeof(AgendaRowView));

    public static readonly BindableProperty MarkCollectedCommandProperty = BindableProperty.Create(
        nameof(MarkCollectedCommand), typeof(ICommand), typeof(AgendaRowView));

    public ICommand? RowTappedCommand
    {
        get => (ICommand?)GetValue(RowTappedCommandProperty);
        set => SetValue(RowTappedCommandProperty, value);
    }

    public ICommand? FillGapCommand
    {
        get => (ICommand?)GetValue(FillGapCommandProperty);
        set => SetValue(FillGapCommandProperty, value);
    }

    public ICommand? MarkCollectedCommand
    {
        get => (ICommand?)GetValue(MarkCollectedCommandProperty);
        set => SetValue(MarkCollectedCommandProperty, value);
    }

    public AgendaRowView()
    {
        InitializeComponent();
    }
}
