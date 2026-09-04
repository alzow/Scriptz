namespace QueueApp.Features.BookingAgenda.Helpers;

public static class BookingAgendaHelper
{
    // How long a pending request has been sitting there. Coarse on purpose: the operator is
    // deciding whether to answer it, not timing it.
    public static string DescribeAge(TimeSpan age)
    {
        if (age.TotalMinutes < 60)
            return $"{Math.Max(1, (int)age.TotalMinutes)} min";

        if (age.TotalHours < 24)
            return (int)age.TotalHours == 1 ? "1 hr" : $"{(int)age.TotalHours} hrs";

        var days = (int)age.TotalDays;
        return days == 1 ? "1 day" : $"{days} days";
    }
}
