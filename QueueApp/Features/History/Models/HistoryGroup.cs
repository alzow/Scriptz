using System.Collections.ObjectModel;

namespace QueueApp.Features.History.Models;

public sealed class HistoryGroup : ObservableCollection<HistoryRow>
{
    public string Title { get; }
    public bool IsUpcoming { get; }

    public HistoryGroup(string title, IEnumerable<HistoryRow> rows) : base(rows)
    {
        Title = title;
        IsUpcoming = title == "UPCOMING";
    }
}
