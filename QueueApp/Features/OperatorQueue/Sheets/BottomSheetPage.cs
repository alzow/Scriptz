using MPowerKit.Popups;

namespace QueueApp.Features.OperatorQueue.Sheets;

// Shared chrome for the board's bottom sheets.
//
// The one structural rule these pages have to obey: Content must be the sheet card itself, never a
// full-screen wrapper around it. MPowerKit decides a tap is "on the background" by testing it
// against the bounds of Content's platform view — anything inside those bounds is treated as a tap
// on the sheet and swallowed. Wrap the card in a Grid that fills the page and every tap lands
// inside Content, SendBackgroundClick never fires, and CloseOnBackgroundClick does nothing.
//
// That leaves the card hugging its own content, so the height cap can't come from a row
// definition. It's applied here instead, once the page knows how tall it is.
public abstract class BottomSheetPage : PopupPage
{
    // Same 88% the design caps sheets at: below it the card hugs its content, past it the
    // ScrollView inside takes over rather than the sheet pushing its own header off the top.
    private const double MaxHeightRatio = 0.88;

    protected BottomSheetPage()
    {
        CloseOnBackgroundClick = true;
        BackgroundColor = Color.FromArgb("#B80A0D12");
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Height > 0 && Content is View card)
            card.MaximumHeightRequest = Height * MaxHeightRatio;
    }
}
