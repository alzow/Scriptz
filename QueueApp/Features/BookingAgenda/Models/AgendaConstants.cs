using Microsoft.Maui.Controls.Shapes;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.BookingAgenda.Models;

public static class AgendaConstants
{
    public const int DayStripLength = 14;

    public const int TickIntervalSeconds = 1;

    public const int RequestUrgentMinutes = 60;

    public const int QuietDayFreeHours = 4;
    public const int QuietDayMaxBookings = 1;

    public const int FallbackServiceMinutes = 15;
    public const int FallbackOpenHour = 9;
    public const int FallbackCloseHour = 17;

    public const double FinishedRowOpacity = 0.62;
    public const double BlockedRowOpacity = 0.72;

    public const string ChevronDown = "ic_chevron_down";
    public const string ChevronUp = "ic_chevron_up";

    public const string EmDash = "—";

    public static DoubleCollection Dashed() => new() { 4, 3 };

    public static DateTimeOffset Sast(DateTime date, TimeSpan time) =>
        new(DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified), LocalTime.Offset);

    public static DateTimeOffset Midnight(DateTime date) => Sast(date, TimeSpan.Zero);
}
