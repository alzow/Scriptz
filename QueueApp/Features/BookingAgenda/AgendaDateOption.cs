using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BookingAgenda;

public sealed class AgendaDateOption : ObservableObject
{
    public DateTime Date { get; }

    public bool IsSelected { get; set; }

    // A purple dot on the chip means requests are waiting on that day. Without it Thursday's
    // pending booking stays invisible until Thursday.
    public bool HasRequests { get; set; }

    public AgendaDateOption(DateTime date)
    {
        Date = date;
        DayText = date.ToString("ddd").ToUpperInvariant();
        DateText = date.Day.ToString();
    }

    public string DayText { get; }
    public string DateText { get; }
}
