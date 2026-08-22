namespace QueueApp.Features.BookingAgenda;

public partial class AgendaDateOption
{
    public DateTime Date { get; }
    public bool IsSelected { get; set; }

    public AgendaDateOption(DateTime date)
    {
        Date = date;
    }

    public string DayLabel => Date == DateTime.Today ? "Today" : Date.ToString("ddd");
    public string DateLabel => Date.ToString("d MMM");
}
