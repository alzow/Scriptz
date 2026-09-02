using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Auth.Helpers;

public partial class AuthFieldView : ContentView
{
    public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
        nameof(LabelText), typeof(string), typeof(AuthFieldView), string.Empty);

    public static readonly BindableProperty HintTextProperty = BindableProperty.Create(
        nameof(HintText), typeof(string), typeof(AuthFieldView), string.Empty);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(AuthFieldView), string.Empty);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AuthFieldView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty InputTypeProperty = BindableProperty.Create(
        nameof(InputType), typeof(Keyboard), typeof(AuthFieldView), Keyboard.Plain);

    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(AuthFieldView), false);

    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
        nameof(ReturnType), typeof(ReturnType), typeof(AuthFieldView), Microsoft.Maui.ReturnType.Done);

    public static readonly BindableProperty ValidatorProperty = BindableProperty.Create(
        nameof(Validator), typeof(IValidator), typeof(AuthFieldView));

    public static readonly BindableProperty SharedStateManagerProperty = BindableProperty.Create(
        nameof(SharedStateManager), typeof(ISharedStateManager), typeof(AuthFieldView));

    public static readonly BindableProperty ValidateOnTextChangedProperty = BindableProperty.Create(
        nameof(ValidateOnTextChanged), typeof(bool), typeof(AuthFieldView), true);

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    public IValidator? Validator
    {
        get => (IValidator?)GetValue(ValidatorProperty);
        set => SetValue(ValidatorProperty, value);
    }

    public ISharedStateManager? SharedStateManager
    {
        get => (ISharedStateManager?)GetValue(SharedStateManagerProperty);
        set => SetValue(SharedStateManagerProperty, value);
    }

    public bool ValidateOnTextChanged
    {
        get => (bool)GetValue(ValidateOnTextChangedProperty);
        set => SetValue(ValidateOnTextChangedProperty, value);
    }

    public AuthFieldView()
    {
        InitializeComponent();
    }
}
