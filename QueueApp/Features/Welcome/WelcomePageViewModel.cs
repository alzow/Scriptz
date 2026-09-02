using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Features.Welcome.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Accessibility;
using QueueApp.Services.Onboarding;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Welcome;

public partial class WelcomePageViewModel : BaseViewModel
{
    #region Constants
    private const string Brand = "Queue";

    private const string PrimaryCta = "Create an account";
    private const string SecondaryCta = "I already have one";
    private const string Footnote = "Free to use. Pay the shop the way you always have.";

    // TODO: the two-line break points below are hand-tuned for English and will not survive a
    // translation pass — see the welcome spec's note on copy in other languages.
    private const string DiscoverNumber = "01";
    private const string DiscoverHeadline = "See what's open\naround you";
    private const string DiscoverBody = "Live waits where there's a queue, and the next free slot where there isn't. Barbers, car washes, clinics.";
    private const string DiscoverArt = "art_welcome_discover.png";

    private const string JoinNumber = "02";
    private const string JoinHeadline = "Take your place\nfrom your couch";
    private const string JoinBody = "Walk-in shops let you join from wherever you are and hold your spot. No standing around, no bench.";
    private const string JoinArt = "art_welcome_join.png";

    private const string BookNumber = "03";
    private const string BookHeadline = "Or book a slot,\nif that's their way";
    private const string BookBody = "Some places work on appointments. Pick a time that suits you and they'll confirm it.";
    private const string BookArt = "art_welcome_book.png";

    private const string LeaveNumber = "04";
    private const string LeaveHeadline = "Either way, we'll say\nwhen to leave";
    private const string LeaveBody = "A nudge that accounts for how long it takes you to get there. Arrive as your turn comes up.";
    private const string LeaveArt = "art_welcome_leave.png";

    private const double AutoAdvanceSeconds = 5;
    #endregion

    #region Properties
    public ObservableCollection<WelcomePanel> Panels { get; } = new();

    public string BrandText => Brand;

    // TODO: the design puts the customer's area here (Queue / LENASIA). It stays empty until the
    // location-permission timing in the welcome spec is settled — asking for a fix on a first-run
    // pitch screen is the harshest possible moment to ask, and a guessed area is worse than none.
    public string EyebrowText => string.Empty;

    public string PrimaryCtaText => PrimaryCta;
    public string SecondaryCtaText => SecondaryCta;
    public string FootnoteText => Footnote;

    public int Position { get; set; }
    #endregion

    #region Fields
    private IDispatcherTimer? _autoAdvanceTimer;
    private bool _isSelfAdvancing;
    private bool _isAutoAdvanceRetired;
    #endregion

    #region Services
    private readonly IFirstRunService _firstRunService;
    private readonly IMotionPreferenceService _motionPreferenceService;
    #endregion

    #region Constructor
    public WelcomePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IFirstRunService firstRunService,
        IMotionPreferenceService motionPreferenceService)
        : base(navigationService, secureStorageService)
    {
        _firstRunService = firstRunService;
        _motionPreferenceService = motionPreferenceService;

        BuildPanels();
    }
    #endregion

    #region Lifecycle
    public override Task OnAppearingAsync()
    {
        try
        {
            _firstRunService.MarkWelcomeSeen();
            StartAutoAdvance();
        }
        catch (Exception exception)
        {
            return HandleExceptionAsync(exception);
        }

        return Task.CompletedTask;
    }

    public override Task OnDisappearingAsync()
    {
        try
        {
            StopAutoAdvance();
        }
        catch (Exception exception)
        {
            return HandleExceptionAsync(exception);
        }

        return Task.CompletedTask;
    }
    #endregion

    public void BuildPanels()
    {
        Panels.Add(new WelcomePanel
        {
            NumberText = DiscoverNumber,
            HeadlineText = DiscoverHeadline,
            BodyText = DiscoverBody,
            IllustrationSource = DiscoverArt,
        });

        Panels.Add(new WelcomePanel
        {
            NumberText = JoinNumber,
            HeadlineText = JoinHeadline,
            BodyText = JoinBody,
            IllustrationSource = JoinArt,
        });

        Panels.Add(new WelcomePanel
        {
            NumberText = BookNumber,
            HeadlineText = BookHeadline,
            BodyText = BookBody,
            IllustrationSource = BookArt,
        });

        Panels.Add(new WelcomePanel
        {
            NumberText = LeaveNumber,
            HeadlineText = LeaveHeadline,
            BodyText = LeaveBody,
            IllustrationSource = LeaveArt,
        });

        SyncActivePanel();
    }

    public void OnPositionChanged()
    {
        try
        {
            SyncActivePanel();

            if (!_isSelfAdvancing)
                RetireAutoAdvance();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void SyncActivePanel()
    {
        for (var index = 0; index < Panels.Count; index++)
            Panels[index].IsActive = index == Position;
    }

    public void StartAutoAdvance()
    {
        if (_isAutoAdvanceRetired || _autoAdvanceTimer is not null)
            return;

        if (_motionPreferenceService.PrefersReducedMotion)
            return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        _autoAdvanceTimer = dispatcher.CreateTimer();
        _autoAdvanceTimer.Interval = TimeSpan.FromSeconds(AutoAdvanceSeconds);
        _autoAdvanceTimer.IsRepeating = true;
        _autoAdvanceTimer.Tick += OnAutoAdvanceTick;
        _autoAdvanceTimer.Start();
    }

    public void StopAutoAdvance()
    {
        if (_autoAdvanceTimer is null)
            return;

        _autoAdvanceTimer.Tick -= OnAutoAdvanceTick;
        _autoAdvanceTimer.Stop();
        _autoAdvanceTimer = null;
    }

    public void RetireAutoAdvance()
    {
        _isAutoAdvanceRetired = true;
        StopAutoAdvance();
    }

    public void OnAutoAdvanceTick(object? sender, EventArgs e)
    {
        try
        {
            if (Position >= Panels.Count - 1)
            {
                StopAutoAdvance();
                return;
            }

            _isSelfAdvancing = true;
            Position++;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
        finally
        {
            _isSelfAdvancing = false;
        }
    }

    [RelayCommand]
    public async Task CreateAccountAsync()
    {
        try
        {
            RetireAutoAdvance();
            await NavigationService.NavigateAsync(NavigationPaths.RegisterPage);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task SignInAsync()
    {
        try
        {
            RetireAutoAdvance();
            await NavigationService.NavigateAsync(
                NavigationPaths.LoginPage,
                new NavigationParameters { { NavigationKeys.CanGoBack, true } });
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
