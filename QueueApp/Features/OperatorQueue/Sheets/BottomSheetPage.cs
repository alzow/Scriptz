using MPowerKit.Popups;

namespace QueueApp.Features.OperatorQueue.Sheets;

public abstract class BottomSheetPage : PopupPage
{
    private const double MaxHeightRatio = 0.88;

    protected BottomSheetPage()
    {
        CloseOnBackgroundClick = true;
        BackgroundColor = (Color)Application.Current!.Resources["ScrimBackground"];
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Height > 0 && Content is View card)
            card.MaximumHeightRequest = Height * MaxHeightRatio;
    }
}
