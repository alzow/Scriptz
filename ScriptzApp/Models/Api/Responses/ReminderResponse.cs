namespace ScriptzApp.Models.Api.Responses;

public class ReminderResponse
{
    public string Id { get; set; } = string.Empty;
    public string MedicationId { get; set; } = string.Empty;
    public MedicationResponse? Medication { get; set; }
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
