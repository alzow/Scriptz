using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Features.BookingAgenda;

public partial class AgendaDateOption : ObservableObject
{
    public DateTime Date { get; }

    [ObservableProperty] private bool _isSelected;

    public AgendaDateOption(DateTime date)
    {
        Date = date;
    }

    public string DayLabel => Date == DateTime.Today ? "Today" : Date.ToString("ddd");
    public string DateLabel => Date.ToString("d MMM");
}
