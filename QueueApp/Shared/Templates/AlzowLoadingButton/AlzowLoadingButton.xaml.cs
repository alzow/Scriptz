using System.Windows.Input;

namespace QueueApp.Shared.Templates.QueueLoadingButton;

public partial class AlzowLoadingButton : ContentView
{
    private const string LOTTIE_NAME = "heartbeat_lottie.json";

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AlzowLoadingButton), default(string), propertyChanged: OnTextChanged);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(AlzowLoadingButton), null);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(AlzowLoadingButton), null);

    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading), typeof(bool), typeof(AlzowLoadingButton), false, propertyChanged: OnIsLoadingChanged, defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty BypassIsLoadingProperty = BindableProperty.Create(
        nameof(BypassIsLoading), typeof(bool), typeof(AlzowLoadingButton), false, defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty DisplayTextProperty = BindableProperty.Create(
        nameof(DisplayText), typeof(string), typeof(AlzowLoadingButton), default(string));

    public static readonly BindableProperty LoadingTextProperty = BindableProperty.Create(
        nameof(LoadingText), typeof(string), typeof(AlzowLoadingButton), default(string));

    public static readonly BindableProperty ButtonStyleProperty = BindableProperty.Create(
        nameof(ButtonStyle), typeof(Style), typeof(AlzowLoadingButton), null);

    public new static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
        nameof(IsEnabled), typeof(bool), typeof(AlzowLoadingButton), true);

    public new static readonly BindableProperty AutomationIdProperty = BindableProperty.Create(
        nameof(AutomationId), typeof(string), typeof(AlzowLoadingButton), null);

    public new static readonly BindableProperty MarginProperty = BindableProperty.Create(
        nameof(Margin), typeof(Thickness), typeof(AlzowLoadingButton), new Thickness(0));

    public static readonly BindableProperty BusyLoaderSourceProperty = BindableProperty.Create(
        nameof(BusyLoaderSource), typeof(string), typeof(AlzowLoadingButton), LOTTIE_NAME, propertyChanged: OnBusyLoaderSourceChanged);

    public static readonly BindableProperty ImageWidthProperty = BindableProperty.Create(
        nameof(ImageWidth), typeof(double), typeof(AlzowLoadingButton), 50.0);

    public static readonly BindableProperty ImageHeightProperty = BindableProperty.Create(
        nameof(ImageHeight), typeof(double), typeof(AlzowLoadingButton), 50.0);

    public AlzowLoadingButton()
    {
        InitializeComponent();
        UpdateDisplayText();
        UpdateImageSize();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public bool BypassIsLoading
    {
        get => (bool)GetValue(BypassIsLoadingProperty);
        set => SetValue(BypassIsLoadingProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    public string LoadingText
    {
        get => (string)GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public Style ButtonStyle
    {
        get => (Style)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public new string AutomationId
    {
        get => (string)GetValue(AutomationIdProperty);
        set => SetValue(AutomationIdProperty, value);
    }

    public new Thickness Margin
    {
        get => (Thickness)GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    public string BusyLoaderSource
    {
        get => (string)GetValue(BusyLoaderSourceProperty);
        set => SetValue(BusyLoaderSourceProperty, value);
    }

    public double ImageWidth
    {
        get => (double)GetValue(ImageWidthProperty);
        private set => SetValue(ImageWidthProperty, value);
    }

    public double ImageHeight
    {
        get => (double)GetValue(ImageHeightProperty);
        private set => SetValue(ImageHeightProperty, value);
    }

    private static void OnBusyLoaderSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AlzowLoadingButton loadingButton)
        {
            loadingButton.UpdateImageSize();
        }
    }

    private void UpdateImageSize()
    {
        if (BusyLoaderSource == LOTTIE_NAME)
        {
            ImageWidth = 24;
            ImageHeight = 24;
        }
        else
        {
            ImageWidth = 24;
            ImageHeight = 24;
        }
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        if (!IsLoading && Command?.CanExecute(CommandParameter) == true)
        {
            if (!BypassIsLoading)
                IsLoading = true;
            await Task.Delay(10);
            Command.Execute(CommandParameter);
        }
    }

    private static void OnIsLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AlzowLoadingButton loadingButton)
        {
            loadingButton.UpdateDisplayText();
        }
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AlzowLoadingButton loadingButton)
        {
            loadingButton.UpdateDisplayText();
        }
    }

    private void UpdateDisplayText()
    {
        DisplayText = IsLoading ? string.Empty : Text;
    }
}
