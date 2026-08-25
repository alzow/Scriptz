using System.Windows.Input;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class LiveQueueHeroView : ContentView
{
    public static readonly BindableProperty ActiveEntryProperty = BindableProperty.Create(
        nameof(ActiveEntry), typeof(MyActiveQueueEntryResponse), typeof(LiveQueueHeroView),
        default(MyActiveQueueEntryResponse), propertyChanged: OnActiveEntryChanged);
    public static readonly BindableProperty DirectionsCommandProperty = BindableProperty.Create(
        nameof(DirectionsCommand), typeof(ICommand), typeof(LiveQueueHeroView), default(ICommand));
    public static readonly BindableProperty LeaveCommandProperty = BindableProperty.Create(
        nameof(LeaveCommand), typeof(ICommand), typeof(LiveQueueHeroView), default(ICommand));
    public static readonly BindableProperty IsLeavingProperty = BindableProperty.Create(
        nameof(IsLeaving), typeof(bool), typeof(LiveQueueHeroView), default(bool));

    public MyActiveQueueEntryResponse ActiveEntry
    {
        get => (MyActiveQueueEntryResponse)GetValue(ActiveEntryProperty);
        set => SetValue(ActiveEntryProperty, value);
    }

    public ICommand DirectionsCommand
    {
        get => (ICommand)GetValue(DirectionsCommandProperty);
        set => SetValue(DirectionsCommandProperty, value);
    }

    public ICommand LeaveCommand
    {
        get => (ICommand)GetValue(LeaveCommandProperty);
        set => SetValue(LeaveCommandProperty, value);
    }

    public bool IsLeaving
    {
        get => (bool)GetValue(IsLeavingProperty);
        set => SetValue(IsLeavingProperty, value);
    }

    private IDispatcherTimer? _timer;
    private int _remainingSeconds;

    public LiveQueueHeroView()
    {
        InitializeComponent();
        Loaded += (_, _) => RestartCountdown();
        Unloaded += (_, _) => StopCountdown();
    }

    public static void OnActiveEntryChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            ((LiveQueueHeroView)bindable).RestartCountdown();
        }
        catch (Exception)
        {
        }
    }

    public void RestartCountdown()
    {
        try
        {
            StopCountdown();

            var minutes = ActiveEntry?.WaitMinutes;
            if (ActiveEntry is null || minutes is null || ActiveEntry.IsBeingServed)
            {
                CountdownLabel.Text = ActiveEntry?.IsBeingServed == true ? "NOW" : "--:--";
                return;
            }

            _remainingSeconds = (int)(minutes.Value * 60);
            UpdateCountdownLabel();

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) =>
            {
                if (_remainingSeconds <= 0)
                {
                    StopCountdown();
                    return;
                }
                _remainingSeconds--;
                UpdateCountdownLabel();
            };
            _timer.Start();
        }
        catch (Exception)
        {
        }
    }

    public void UpdateCountdownLabel()
    {
        try
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
            CountdownLabel.Text = _remainingSeconds <= 0 ? "GO" : $"{span.Minutes:00}:{span.Seconds:00}";
            CountdownLabel.TextColor = _remainingSeconds <= 0
                ? (Color)Application.Current!.Resources["Green"]
                : (Color)Application.Current!.Resources["TextPrimary"];
        }
        catch (Exception)
        {
        }
    }

    public void StopCountdown()
    {
        try
        {
            if (_timer is null) return;
            _timer.Stop();
            _timer = null;
        }
        catch (Exception)
        {
        }
    }
}
