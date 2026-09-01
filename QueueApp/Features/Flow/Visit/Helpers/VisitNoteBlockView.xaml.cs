namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitNoteBlockView : ContentView
{
    public static readonly BindableProperty NoteTitleProperty = BindableProperty.Create(
        nameof(NoteTitle), typeof(string), typeof(VisitNoteBlockView), string.Empty);

    public static readonly BindableProperty NoteTextProperty = BindableProperty.Create(
        nameof(NoteText), typeof(string), typeof(VisitNoteBlockView), string.Empty);

    public static readonly BindableProperty IsFromShopProperty = BindableProperty.Create(
        nameof(IsFromShop), typeof(bool), typeof(VisitNoteBlockView), false);

    public string NoteTitle
    {
        get => (string)GetValue(NoteTitleProperty);
        set => SetValue(NoteTitleProperty, value);
    }

    public string NoteText
    {
        get => (string)GetValue(NoteTextProperty);
        set => SetValue(NoteTextProperty, value);
    }

    public bool IsFromShop
    {
        get => (bool)GetValue(IsFromShopProperty);
        set => SetValue(IsFromShopProperty, value);
    }

    public VisitNoteBlockView()
    {
        InitializeComponent();
    }
}
