using System.Windows.Input;
using System.Runtime.CompilerServices;
using QueueApp.Shared.Templates.AlzowEntry.Validators;


#if ANDROID
using Android.Content.Res;
#endif


namespace QueueApp.Shared.Templates.AlzowEntry;

public partial class AlzowEntry : ContentView, IValidationView
{
    public static readonly BindableProperty EntryTextProperty = BindableProperty.Create(nameof(EntryText), typeof(string), typeof(AlzowEntry), default(string), BindingMode.TwoWay, propertyChanged: OnInputTextChanged);
    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(nameof(PlaceholderText), typeof(string), typeof(AlzowEntry), default(string));
    public static readonly BindableProperty RightIconProperty = BindableProperty.Create(nameof(RightIcon), typeof(string), typeof(AlzowEntry), default(string));
    public static readonly BindableProperty LeftIconProperty = BindableProperty.Create(nameof(LeftIcon), typeof(string), typeof(AlzowEntry), default(string));
    public static readonly BindableProperty LeftEmojiIconProperty = BindableProperty.Create(nameof(LeftEmojiIcon), typeof(string), typeof(AlzowEntry), default(string));
    public static readonly BindableProperty TextChangedCommandProperty = BindableProperty.Create(nameof(TextChangedCommand), typeof(ICommand), typeof(AlzowEntry), default(ICommand));
    public static readonly BindableProperty ShowClearTextButtonProperty = BindableProperty.Create(nameof(ShowClearTextButton), typeof(bool), typeof(AlzowEntry), default(bool));
    public static readonly BindableProperty ShowIconOnFocusProperty = BindableProperty.Create(nameof(ShowIconOnFocus), typeof(bool), typeof(AlzowEntry), default(bool));
    public static readonly BindableProperty ValidatorProperty = BindableProperty.Create(nameof(Validator), typeof(IValidator), typeof(AlzowEntry), default(IValidator));
    public static readonly BindableProperty ValidatorsProperty = BindableProperty.Create(nameof(Validators), typeof(IList<IValidator>), typeof(AlzowEntry), default(IList<IValidator>));
    public static readonly BindableProperty IsValidProperty = BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(AlzowEntry), default(bool), BindingMode.OneWayToSource, propertyChanged: OnIsValidChanged);
    public static readonly BindableProperty ValidateOnTextChangedProperty = BindableProperty.Create(nameof(ValidateOnTextChanged), typeof(bool), typeof(AlzowEntry), default(bool));
    public static readonly BindableProperty ValidationStatusProperty = BindableProperty.Create(nameof(ValidationStatus), typeof(ValidationState), typeof(AlzowEntry), ValidationState.None, BindingMode.OneWayToSource);
    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(AlzowEntry), Colors.LightGray, BindingMode.TwoWay);
    public static readonly BindableProperty RightIconCommandProperty = BindableProperty.Create(nameof(RightIconCommand), typeof(ICommand), typeof(AlzowEntry), default(ICommand));
    public static readonly BindableProperty ClickCommandProperty = BindableProperty.Create(nameof(ClickCommand), typeof(ICommand), typeof(AlzowEntry), default(ICommand));
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(AlzowEntry), default(bool));
    public static readonly BindableProperty InputTypeProperty = BindableProperty.Create(nameof(InputType), typeof(Keyboard), typeof(AlzowEntry), Keyboard.Plain);
    public static readonly BindableProperty KeepValidOnErrorProperty = BindableProperty.Create(nameof(KeepValidOnError), typeof(bool), typeof(AlzowEntry), false);
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(AlzowEntry), defaultValue: 256);
    public static readonly BindableProperty SharedStateManagerProperty = BindableProperty.Create(nameof(SharedStateManager), typeof(ISharedStateManager), typeof(AlzowEntry), defaultValue: null);
    public static readonly BindableProperty IsEntryFocusedProperty = BindableProperty.Create(nameof(IsEntryFocused), typeof(bool), typeof(AlzowEntry), defaultValue: false, BindingMode.OneWayToSource);
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(AlzowEntry), ReturnType.Done, propertyChanged: OnReturnTypeChanged);
    public static readonly BindableProperty IsReturnButtonEnabledProperty = BindableProperty.Create(nameof(IsReturnButtonEnabled), typeof(bool), typeof(AlzowEntry), true, propertyChanged: OnReturnButtonEnabledChanged);
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(AlzowEntry), default(bool), propertyChanged: OnIsPasswordChanged);

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }
    public string EntryText
    {
        get => (string)GetValue(EntryTextProperty);
        set => SetValue(EntryTextProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public ICommand TextChangedCommand
    {
        get => (ICommand)GetValue(TextChangedCommandProperty);
        set => SetValue(TextChangedCommandProperty, value);
    }

    public string RightIcon
    {
        get => (string)GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    public string LeftEmojiIcon
    {
        get => (string)GetValue(LeftEmojiIconProperty);
        set => SetValue(LeftEmojiIconProperty, value);
    }
    public string LeftIcon
    {
        get => (string)GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    public bool ShowClearTextButton
    {
        get => (bool)GetValue(ShowClearTextButtonProperty);
        set => SetValue(ShowClearTextButtonProperty, value);
    }

    public bool ShowIconOnFocus
    {
        get => (bool)GetValue(ShowIconOnFocusProperty);
        set => SetValue(ShowIconOnFocusProperty, value);
    }

    public IValidator Validator
    {
        get => (IValidator)GetValue(ValidatorProperty);
        set => SetValue(ValidatorProperty, value);
    }

    public IList<IValidator> Validators
    {
        get => (IList<IValidator>)GetValue(ValidatorsProperty);
        set => SetValue(ValidatorsProperty, value);
    }

    public bool IsValid
    {
        get => (bool)GetValue(IsValidProperty);
        set => SetValue(IsValidProperty, value);
    }

    public bool ValidateOnTextChanged
    {
        get => (bool)GetValue(ValidateOnTextChangedProperty);
        set => SetValue(ValidateOnTextChangedProperty, value);
    }

    public ValidationState ValidationStatus
    {
        get => (ValidationState)GetValue(ValidationStatusProperty);
        set => SetValue(ValidationStatusProperty, value);
    }

    public ICommand RightIconCommand
    {
        get => (ICommand)GetValue(RightIconCommandProperty);
        set => SetValue(RightIconCommandProperty, value);
    }

    public ICommand ClickCommand
    {
        get => (ICommand)GetValue(ClickCommandProperty);
        set => SetValue(ClickCommandProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public Keyboard InputType
    {
        get => (Keyboard)GetValue(InputTypeProperty);
        set => SetValue(InputTypeProperty, value);
    }

    public bool KeepValidOnError
    {
        get => (bool)GetValue(KeepValidOnErrorProperty);
        set => SetValue(KeepValidOnErrorProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public ISharedStateManager SharedStateManager
    {
        get => (ISharedStateManager)GetValue(SharedStateManagerProperty);
        set => SetValue(SharedStateManagerProperty, value);
    }

    public bool IsEntryFocused
    {
        get => (bool)GetValue(IsEntryFocusedProperty);
        set => SetValue(IsEntryFocusedProperty, value);
    }

    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    public bool IsReturnButtonEnabled
    {
        get => (bool)GetValue(IsReturnButtonEnabledProperty);
        set => SetValue(IsReturnButtonEnabledProperty, value);
    }

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    private string _errorMessage = string.Empty;

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (value != _errorMessage)
            {
                _errorMessage = value;
            }
        }
    }

    public event EventHandler<bool> ValidationChanged;
    public event EventHandler<FocusEventArgs> OnFocused;
    public event EventHandler<FocusEventArgs> OnUnfocused;

    public AlzowEntry()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            SharedStateManager?.RegisterEntry(this);
            UpdateReturnButtonHandler();
        };
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("AlzowEntryControl", (handler, view) =>
        {
#if IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });
    }

    ~AlzowEntry()
    {
        SharedStateManager?.UnregisterEntry(this);
    }

    private static void OnIsValidChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AlzowEntry)bindable;
        var isControlValid = (bool)newValue || !control.IsVisible;
        control.ValidationChanged?.Invoke(control, isControlValid);
    }

    private static void OnInputTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AlzowEntry)bindable;

        if (string.IsNullOrEmpty(newValue?.ToString()))
        {
            if (control.ShowClearTextButton)
            {
                control.ClearButton.IsVisible = false;
            }
            control.ControlPlaceholderLabel.OnUnFocusAnimation(control.ControlRightIcon, control.AlzowEntryControl);
        }
        else
        {
            control.ControlPlaceholderLabel.OnFocusAnimation(control.ControlRightIcon, control.AlzowEntryControl, control.ShowIconOnFocus);
        }

        control.UpdateReturnButtonState();
    }

    public void SetClearTextButtonVisibility(bool isVisible)
    {
        ClearButton.IsVisible = isVisible;
    }

    private static void OnReturnButtonEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AlzowEntry)bindable;
        control.UpdateReturnButtonHandler();
    }

    private static void OnReturnTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AlzowEntry)bindable;
        control.UpdateReturnButtonState();
        control.UpdateReturnButtonHandler();
    }

    private static void OnIsPasswordChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (AlzowEntry)bindable;
        control.AlzowEntryControl.IsPassword = (bool)newValue;
    }


    public void SetEntryReadOnly(bool isReadOnly)
    {
        AlzowEntryControl.IsReadOnly = isReadOnly;
    }

    public void SetRightIcon(string icon)
    {
        ControlRightIcon.Source = icon;
    }

    public Entry GetEntryControl()
    {
        return AlzowEntryControl;
    }

    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        IsEntryFocused = true;
        ControlPlaceholderLabel.OnFocusAnimation(ControlRightIcon, AlzowEntryControl, ShowIconOnFocus);
        OnFocused?.Invoke(this, e);
    }

    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        IsEntryFocused = false;
        if (string.IsNullOrEmpty(AlzowEntryControl.Text))
        {
            ControlPlaceholderLabel.OnUnFocusAnimation(ControlRightIcon, AlzowEntryControl);
            Validate(AlzowEntryControl.Text);
        }
        else
        {
            Validate(AlzowEntryControl.Text);
        }
        if (Parent is VisualElement parentElement)
        {
            parentElement.Unfocus();
            OnUnfocused?.Invoke(this, e);
        }
    }

    protected void SetValidateOnTextChanged(bool validateOnTextChanged)
    {
        ValidateOnTextChanged = validateOnTextChanged;
    }

    private async void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.NewTextValue == e.OldTextValue || !this.IsActuallyVisible() || (string.IsNullOrWhiteSpace(e.NewTextValue) && string.IsNullOrWhiteSpace(e.OldTextValue))) return;

        if (ShowClearTextButton)
        {
            ClearButton.IsVisible = true;
        }

        if (TextChangedCommand != null)
        {
            TextChangedCommand.Execute(e.NewTextValue);
        }

        if (ValidateOnTextChanged)
        {
            if (Validator != null && Validator.IsAsync)
            {
                await ValidateAsyncValidator(e.NewTextValue);
            }
            else
            {
                Validate(e.NewTextValue);
            }
        }

        if (string.IsNullOrEmpty(e.NewTextValue))
        {
            if (ShowClearTextButton)
            {
                ClearButton.IsVisible = false;
            }
            ControlPlaceholderLabel.OnUnFocusAnimation(ControlRightIcon, AlzowEntryControl);
        }
        else
        {
            ControlPlaceholderLabel.OnFocusAnimation(ControlRightIcon, AlzowEntryControl, ShowIconOnFocus);
        }

        UpdateReturnButtonState();
    }

    private void UpdateReturnButtonState()
    {
        if (ReturnType == ReturnType.Search)
        {
            IsReturnButtonEnabled = !string.IsNullOrEmpty(EntryText);
        }
        else
        {
            IsReturnButtonEnabled = true;
        }
    }

    private void UpdateReturnButtonHandler()
    {
#if IOS || MACCATALYST
        if (AlzowEntryControl?.Handler?.PlatformView is UIKit.UITextField textField)
        {
            textField.EnablesReturnKeyAutomatically = ReturnType == ReturnType.Search;
        }
#endif
        #if ANDROID
                if (AlzowEntryControl?.Handler?.PlatformView is AndroidX.AppCompat.Widget.AppCompatEditText editText)
                {
                    var imeAction = IsReturnButtonEnabled ?
                        Android.Views.InputMethods.ImeAction.Search :
                        Android.Views.InputMethods.ImeAction.None;

                    editText.ImeOptions = imeAction;
                }
        #endif
    }

    private void SetErrorMessage(string errorMessage)
    {
        InlineMessageLabel.Text = errorMessage;
        ToggleMessageView(true, errorMessage);
    }

    public void ClearErrorState(bool isReset = false)
    {
        ValidationStatus = ValidationState.None;
        InlineMessageLabel.Text = string.Empty;
        ToggleMessageView(false, "");

        if (isReset)
        {
            AlzowEntryFrame.BorderColor = Color.Parse("#E1E1E1");
            IsValid = false;
        }
    }

    // Public helper to surface a validation error originating outside of the configured validators
    public void ShowValidationError(string errorMessage, bool keepValidOnError = false)
    {
        KeepValidOnError = keepValidOnError;
        IsValid = keepValidOnError;
        SetErrorMessage(errorMessage);
        UpdateEffectsIsValid(IsValid);
        UpdateValidationState(IsValid);
    }

    public void SetValidator(IValidator validator)
    {
        Validator = validator;
    }

    public void SetValidators(IList<IValidator> validators)
    {
        Validators = validators;
    }

    public void AddValidator(IValidator validator)
    {
        Validators?.Add(validator);
    }

    public void RemoveValidator(IValidator validator)
    {
        Validators?.Remove(validator);
    }

    public void RemoveValidatorByType(Type type)
    {
        (Validators as List<IValidator>)?.RemoveAll(validator => validator.GetType() == type);
    }



    public bool Validate(string value)
    {
        if (Validator == null && Validators == null || !this.IsActuallyVisible())
        {
            return true;
        }

        if (Validators != null)
        {
            IsValid = CheckValidators(value);
        }
        else
        {
            KeepValidOnError = !Validator.IsBlocking;
            IsValid = Validator.Validate(value);
        }

        if (IsValid)
        {
            ClearErrorState();
        }
        else if (KeepValidOnError && !IsValid)
        {
            HandleValidationError();
        }
        else
        {
            HandleValidationError();
        }

        UpdateEffectsIsValid(IsValid);
        UpdateValidationState(IsValid);

        return KeepValidOnError || IsValid;
    }

    private async Task<bool> ValidateAsyncValidator(string value)
    {
        KeepValidOnError = !Validator.IsBlocking;

        IsValid = await Validator.ValidateAsync(value);
        if (IsValid)
        {
            ClearErrorState();
        }
        else if (KeepValidOnError && !IsValid)
        {
            HandleValidationError();
        }
        else
        {
            HandleValidationError();
        }

        UpdateEffectsIsValid(IsValid);
        UpdateValidationState(IsValid);

        return KeepValidOnError || IsValid;
    }

    public async Task<bool> ValidateAsync(string value)
    {
        if (Validator == null && Validators == null || !this.IsActuallyVisible())
        {
            return true;
        }

        if (Validators != null)
        {
            IsValid = CheckValidators(value);
        }
        else
        {
            KeepValidOnError = !Validator.IsBlocking;
            if (Validator.IsAsync)
            {
                IsValid = await Validator.ValidateAsync(value);
            }
            else
            {
                IsValid = Validator.Validate(value);
            }
        }

        if (IsValid)
        {
            ClearErrorState();
        }
        else if (KeepValidOnError && !IsValid)
        {
            HandleValidationError();
        }
        else
        {
            HandleValidationError();
        }

        UpdateEffectsIsValid(IsValid);
        UpdateValidationState(IsValid);

        return KeepValidOnError || IsValid;
    }

    public bool Validate()
    {
        var isValid = Validate(AlzowEntryControl.Text);
        UpdateEffectsIsValid(isValid);
        return isValid;
    }

    private void HandleValidationError()
    {
        if (Validators == null)
        {
            SetErrorMessage(Validator.ErrorMessage);
        }
    }

    public async Task<bool> ValidateAsync()
    {
        var isValid = await ValidateAsync(AlzowEntryControl.Text);
        UpdateEffectsIsValid(isValid);
        return isValid;
    }

    private void UpdateEffectsIsValid(bool isValid)
    {
        if (IsValid)
        {
            AlzowEntryFrame.BorderColor = Color.Parse("#009639");
        }
        else if (!isValid && KeepValidOnError)
        {
            AlzowEntryFrame.BorderColor = Color.Parse("#DADDDB");
        }
        else if (!isValid)
        {
            AlzowEntryFrame.BorderColor = Color.Parse("#E70027");
        }
    }

    private bool CheckValidators(string value)
    {
        var hasBlockingValidators = Validators.Any(validator => validator.IsBlocking);
        if (hasBlockingValidators)
        {
            var blockingValidatoValidators = Validators.Where(validator => validator.IsBlocking).ToList();

            foreach (var blockingValidator in blockingValidatoValidators)
            {

                if (!blockingValidator.Validate(value))
                {
                    _errorMessage = blockingValidator.ErrorMessage;
                    SetErrorMessage(blockingValidator.ErrorMessage);
                    KeepValidOnError = false;
                    return false;
                }
            }
        }

        var hasNonBlockingValidators = Validators.Any(validator => !validator.IsBlocking);
        if (hasNonBlockingValidators)
        {
            var nonBlockingValidatoValidators = Validators.Where(validator => !validator.IsBlocking).ToList();
            foreach (var nonBlockingalidator in nonBlockingValidatoValidators)
            {
                if (!nonBlockingalidator.Validate(value))
                {
                    _errorMessage = nonBlockingalidator.ErrorMessage;
                    SetErrorMessage(nonBlockingalidator.ErrorMessage);
                    KeepValidOnError = true;
                    return false;
                }
            }
        }

        return true;
    }

    private void UpdateValidationState(bool isValid)
    {
        if (isValid)
        {
            ValidationStatus = ValidationState.Valid;
        }
        else if (!IsValid && KeepValidOnError)
        {
            ValidationStatus = ValidationState.Warning;
        }
        else
        {
            ValidationStatus = ValidationState.Error;
        }
    }

    private void ToggleMessageView(bool isVisible, string errorMessage)
    {
        InlineMessageView.IsVisible = isVisible && !string.IsNullOrEmpty(errorMessage);
    }

    private void ClearButton_Clicked(object sender, EventArgs e)
    {
        AlzowEntryControl.Text = string.Empty;
        AlzowEntryControl.Unfocus();
        RightIconCommand?.Execute(null);
    }

    public void UnfocusAlzowEntry()
    {
        AlzowEntryControl.Unfocus();
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName != null && propertyName == nameof(IsVisible))
        {
            SharedStateManager?.ValidateOnViewUpdate();
        }
    }
}
