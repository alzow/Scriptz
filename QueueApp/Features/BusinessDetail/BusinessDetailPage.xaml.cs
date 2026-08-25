namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPage : ContentPage
{
    private bool _stickyShown;

    public BusinessDetailPage()
    {
        InitializeComponent();
        LandingScroll.Scrolled += OnLandingScrolled;
    }

    // Android's hardware back has to match the on-screen one — without this it pops the whole page
    // from step three instead of stepping back through the flow.
    protected override bool OnBackButtonPressed()
    {
        return BindingContext is BusinessDetailPageViewModel vm && vm.TryHandleHardwareBack()
            || base.OnBackButtonPressed();
    }

    // The card's CTA and the sticky bar are the same action, so only one is ever on screen: the bar
    // fades in once the live card has scrolled past and back out when it returns.
    private async void OnLandingScrolled(object? sender, ScrolledEventArgs e)
    {
        if (BindingContext is not BusinessDetailPageViewModel vm)
            return;

        var shouldShow = e.ScrollY > LiveCard.Y + LiveCard.Height;
        if (shouldShow == _stickyShown)
            return;

        _stickyShown = shouldShow;

        if (shouldShow)
        {
            vm.IsStickyCtaVisible = true;
            await StickyCta.FadeTo(1, 160);
        }
        else
        {
            await StickyCta.FadeTo(0, 160);

            // A fast scroll back down can flip this again mid-fade; only the latest wins.
            if (!_stickyShown)
                vm.IsStickyCtaVisible = false;
        }
    }
}
