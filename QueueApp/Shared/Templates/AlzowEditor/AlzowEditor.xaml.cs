namespace QueueApp.Shared.Templates.AlzowEditor;

// The multi-line half of the ALZOW entry: the same 16px shell, the same focus stroke, so a note
// field and a name field on the same screen read as one control family.
public partial class AlzowEditor : ContentView
{
    private const string BorderStyleKey = "brd16_Editor";
    private const string FocusedBorderStyleKey = "brd16_Editor_Focused";

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AlzowEditor), default(string), BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(AlzowEditor), default(string));

    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
        nameof(MaxLength), typeof(int), typeof(AlzowEditor), 500);

    public static readonly BindableProperty IsEditorEnabledProperty = BindableProperty.Create(
        nameof(IsEditorEnabled), typeof(bool), typeof(AlzowEditor), true);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public bool IsEditorEnabled
    {
        get => (bool)GetValue(IsEditorEnabledProperty);
        set => SetValue(IsEditorEnabledProperty, value);
    }

    public AlzowEditor()
    {
        InitializeComponent();
    }

    public void OnEditorFocused(object sender, FocusEventArgs e) => ApplyBorderStyle(FocusedBorderStyleKey);

    public void OnEditorUnfocused(object sender, FocusEventArgs e) => ApplyBorderStyle(BorderStyleKey);

    private void ApplyBorderStyle(string key)
    {
        try
        {
            if (Application.Current?.Resources.TryGetValue(key, out var style) is true && style is Style resolved)
                EditorBorder.Style = resolved;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Could not apply the editor border style: {exception.Message}");
        }
    }
}
