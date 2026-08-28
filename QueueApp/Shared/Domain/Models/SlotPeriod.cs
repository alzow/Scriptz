using System.Collections.ObjectModel;

namespace QueueApp.Shared.Domain.Models;

// One of MORNING / AFTERNOON / EVENING on the time step. An empty period keeps its header and gets
// an explanation instead of collapsing — a missing evening on a three-hour job reads as a bug
// unless something says why it's missing.
public sealed class SlotPeriod
{
    private const double RowHeight = 50;
    private const int Columns = 4;

    public SlotPeriod(string title, IReadOnlyList<SlotChoiceItem> slots, string emptyNote)
    {
        Title = title;
        Slots = new ObservableCollection<SlotChoiceItem>(slots);
        CountText = slots.Count > 0 ? $"{slots.Count} available" : emptyNote;
        GridHeight = Math.Ceiling(slots.Count / (double)Columns) * RowHeight;
    }

    public string Title { get; }
    public ObservableCollection<SlotChoiceItem> Slots { get; }
    public string CountText { get; }

    // The grid sits inside the step's ScrollView, so it needs a real height rather than a nested
    // scroll region of its own.
    public double GridHeight { get; }

    public bool HasSlots => Slots.Count > 0;
    public bool IsEmpty => Slots.Count == 0;
}
