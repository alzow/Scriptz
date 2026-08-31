using System.Windows.Input;
using QueueApp.Features.CategoryPicker.Models;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class LocationBarView : ContentView
{
    private const string PulseAnimationName = "LocationBarPulse";
    private const uint PulseCycleMilliseconds = 900;
    private const uint CrossfadeStepMilliseconds = 90;

    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(LocationBarState), typeof(LocationBarView), LocationBarState.Resolving,
        propertyChanged: OnStateChanged);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(LocationBarView), string.Empty);

    public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create(
        nameof(RefreshCommand), typeof(ICommand), typeof(LocationBarView));

    public LocationBarState State
    {
        get => (LocationBarState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public bool ShowTrailingIcon => State != LocationBarState.Resolving;
    public string TrailingIcon => State == LocationBarState.Failed ? "ic_refresh" : "ic_chevron_down";

    public Style TextStyle => (Style)Application.Current!.Resources[
        State is LocationBarState.Denied or LocationBarState.Failed ? "lbl15_Medium_Danger" : "lbl15_Medium_TextInk"];

    public LocationBarView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is null)
        {
            this.AbortAnimation(PulseAnimationName);
        }
        else if (State == LocationBarState.Resolving)
        {
            StartPulse();
        }
    }

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (LocationBarView)bindable;
        view.OnPropertyChanged(nameof(ShowTrailingIcon));
        view.OnPropertyChanged(nameof(TrailingIcon));
        view.OnPropertyChanged(nameof(TextStyle));

        var wasResolving = (LocationBarState)oldValue == LocationBarState.Resolving;
        var isResolving = (LocationBarState)newValue == LocationBarState.Resolving;

        if (isResolving)
        {
            view.StartPulse();
        }
        else
        {
            view.StopPulse();
            if (wasResolving)
                view.CrossfadeIn();
        }
    }

    private void StartPulse()
    {
        if (Handler is null || this.AnimationIsRunning(PulseAnimationName))
            return;

        var pulse = new Animation();
        pulse.Add(0.0, 0.5, new Animation(v => LocationLabel.Opacity = v, 1.0, 0.45, Easing.SinInOut));
        pulse.Add(0.5, 1.0, new Animation(v => LocationLabel.Opacity = v, 0.45, 1.0, Easing.SinInOut));

        pulse.Commit(this, PulseAnimationName,
            length: PulseCycleMilliseconds,
            repeat: () => State == LocationBarState.Resolving,
            finished: (_, _) => LocationLabel.Opacity = 1.0);
    }

    private void StopPulse()
    {
        this.AbortAnimation(PulseAnimationName);
        LocationLabel.Opacity = 1.0;
    }

    private async void CrossfadeIn()
    {
        try
        {
            await LocationLabel.FadeTo(0, CrossfadeStepMilliseconds, Easing.CubicOut);
            await LocationLabel.FadeTo(1, CrossfadeStepMilliseconds, Easing.CubicIn);
        }
        catch (Exception)
        {
        }
    }
}
