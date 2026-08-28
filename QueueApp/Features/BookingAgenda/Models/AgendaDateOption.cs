using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class AgendaDateOption : ObservableObject
{
    public DateTime Date { get; }

    public bool IsSelected { get; set; }
    public bool HasRequests { get; set; }

    public string DayText { get; }
    public string DateText { get; }

    public AgendaDateOption(DateTime date)
    {
        Date = date;
        DayText = date.ToString("ddd").ToUpperInvariant();
        DateText = date.Day.ToString();
    }
}
