using MPowerKit.Popups;
using QueueApp.Framework.Theming;

namespace QueueApp.Shared.Templates.BottomSheet;

public abstract class BottomSheetPage : PopupPage
{
    private const double MaxHeightRatio = 0.88;

    protected BottomSheetPage()
    {
        CloseOnBackgroundClick = true;
        BackgroundColor = ThemePalette.Scrim;
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Height > 0 && Content is View card)
            card.MaximumHeightRequest = Height * MaxHeightRatio;
    }
}
