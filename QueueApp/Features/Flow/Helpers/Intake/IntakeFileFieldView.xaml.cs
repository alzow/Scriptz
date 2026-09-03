using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeFileFieldView : ContentView
{
    public static readonly BindableProperty FileNameProperty = BindableProperty.Create(
        nameof(FileName), typeof(string), typeof(IntakeFileFieldView), string.Empty);

    public static readonly BindableProperty FileSizeTextProperty = BindableProperty.Create(
        nameof(FileSizeText), typeof(string), typeof(IntakeFileFieldView), string.Empty);

    public static readonly BindableProperty HasFileProperty = BindableProperty.Create(
        nameof(HasFile), typeof(bool), typeof(IntakeFileFieldView), false);

    public static readonly BindableProperty PickTextProperty = BindableProperty.Create(
        nameof(PickText), typeof(string), typeof(IntakeFileFieldView), "Choose a file");

    public static readonly BindableProperty IsPickingProperty = BindableProperty.Create(
        nameof(IsPicking), typeof(bool), typeof(IntakeFileFieldView), false);

    public static readonly BindableProperty PickCommandProperty = BindableProperty.Create(
        nameof(PickCommand), typeof(ICommand), typeof(IntakeFileFieldView));

    public static readonly BindableProperty ClearCommandProperty = BindableProperty.Create(
        nameof(ClearCommand), typeof(ICommand), typeof(IntakeFileFieldView));

    // The field row this view is rendering, handed to both commands: the view model owns which
    // answer changed, this view only says that one did.
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(IntakeFileFieldView));

    public string FileName
    {
        get => (string)GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    public string FileSizeText
    {
        get => (string)GetValue(FileSizeTextProperty);
        set => SetValue(FileSizeTextProperty, value);
    }

    public bool HasFile
    {
        get => (bool)GetValue(HasFileProperty);
        set => SetValue(HasFileProperty, value);
    }

    public string PickText
    {
        get => (string)GetValue(PickTextProperty);
        set => SetValue(PickTextProperty, value);
    }

    public bool IsPicking
    {
        get => (bool)GetValue(IsPickingProperty);
        set => SetValue(IsPickingProperty, value);
    }

    public ICommand? PickCommand
    {
        get => (ICommand?)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IntakeFileFieldView()
    {
        InitializeComponent();
    }
}
