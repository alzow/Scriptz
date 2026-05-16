namespace ScriptzApp.Models.Api.Requests;

public class CreateReminderRequest
{
    public string MedicationId { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

public class UpdateReminderRequest : CreateReminderRequest
{
}
