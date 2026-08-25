using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Shared.Templates.QueueEntry;

public partial class QueueEntry : ContentView, IValidationView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(QueueEntry), default(string), BindingMode.TwoWay, propertyChanged: OnTextPropertyChanged);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(QueueEntry), default(string));
    public static readonly BindableProperty LeftIconProperty = BindableProperty.Create(nameof(LeftIcon), typeof(string), typeof(QueueEntry), default(string), propertyChanged: OnLeftIconChanged);
    public static readonly BindableProperty ShowClearTextButtonProperty = BindableProperty.Create(nameof(ShowClearTextButton), typeof(bool), typeof(QueueEntry), default(bool));
    public static readonly BindableProperty InputTypeProperty = BindableProperty.Create(nameof(InputType), typeof(Keyboard), typeof(QueueEntry), Keyboard.Plain);
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(QueueEntry), default(bool), propertyChanged: OnIsPasswordChanged);
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(QueueEntry), ReturnType.Done);
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(QueueEntry), 256);
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(QueueEntry), default(bool), propertyChanged: OnIsReadOnlyChanged);
    public static readonly BindableProperty ValidatorProperty = BindableProperty.Create(nameof(Validator), typeof(IValidator), typeof(QueueEntry), default(IValidator));
    public static readonly BindableProperty ValidateOnTextChangedProperty = BindableProperty.Create(nameof(ValidateOnTextChanged), typeof(bool), typeof(QueueEntry), default(bool));
    public static readonly BindableProperty IsValidProperty = BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(QueueEntry), default(bool), BindingMode.OneWayToSource);

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

    public string LeftIcon
    {
        get => (string)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    public bool HasLeftIcon => !string.IsNullOrEmpty(LeftIcon);

    public bool ShowClearTextButton
    {
        get => (bool)GetValue(ShowClearTextButtonProperty);
        set => SetValue(ShowClearTextButtonProperty, value);
    }

    public Keyboard InputType
    {
        get => (Keyboard)GetValue(InputTypeProperty);
        set => SetValue(InputTypeProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public new bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public IValidator Validator
    {
        get => (IValidator)GetValue(ValidatorProperty);
        set => SetValue(ValidatorProperty, value);
    }

    public bool ValidateOnTextChanged
    {
        get => (bool)GetValue(ValidateOnTextChangedProperty);
        set => SetValue(ValidateOnTextChangedProperty, value);
    }

    public bool IsValid
    {
        get => (bool)GetValue(IsValidProperty);
        private set => SetValue(IsValidProperty, value);
    }

    public string ErrorMessage { get; private set; } = string.Empty;

    public event EventHandler<bool> ValidationChanged;

    public QueueEntry()
    {
        InitializeComponent();
    }

    private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (QueueEntry)bindable;
        control.ClearButton.IsVisible = control.ShowClearTextButton && !string.IsNullOrEmpty(newValue?.ToString());
    }

    private static void OnLeftIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((QueueEntry)bindable).OnPropertyChanged(nameof(HasLeftIcon));
    }

    private static void OnIsPasswordChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((QueueEntry)bindable).QueueEntryControl.IsPassword = (bool)newValue;
    }

    private static void OnIsReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((QueueEntry)bindable).QueueEntryControl.IsReadOnly = (bool)newValue;
    }

    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        QueueEntryBorder.Stroke = (Color)Application.Current.Resources["Purple"];
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        Validate(QueueEntryControl.Text);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ClearButton.IsVisible = ShowClearTextButton && !string.IsNullOrEmpty(e.NewTextValue);

        if (ValidateOnTextChanged)
        {
            Validate(e.NewTextValue);
        }
    }

    private void OnClearButtonClicked(object sender, EventArgs e)
    {
        QueueEntryControl.Text = string.Empty;
        QueueEntryControl.Unfocus();
    }

    public bool Validate() => Validate(QueueEntryControl.Text);

    public Task<bool> ValidateAsync() => Task.FromResult(Validate());

    public bool Validate(string value)
    {
        if (Validator == null || !this.IsActuallyVisible())
        {
            return true;
        }

        IsValid = Validator.Validate(value);

        if (IsValid)
        {
            ClearErrorState();
        }
        else
        {
            ErrorMessage = Validator.ErrorMessage;
            ErrorLabel.Text = ErrorMessage;
            ErrorLabel.IsVisible = true;
            QueueEntryBorder.Stroke = (Color)Application.Current.Resources["Danger"];
        }

        ValidationChanged?.Invoke(this, IsValid);
        return IsValid;
    }

    public void ClearErrorState(bool isReset = false)
    {
        ErrorLabel.Text = string.Empty;
        ErrorLabel.IsVisible = false;
        QueueEntryBorder.Stroke = (Color)Application.Current.Resources["Line"];

        if (isReset)
        {
            IsValid = false;
        }
    }
}
