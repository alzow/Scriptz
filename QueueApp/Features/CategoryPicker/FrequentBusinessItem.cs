namespace QueueApp.Features.CategoryPicker;

// Computed client-side in CategoryPickerPageViewModel by grouping GetMyVisitsAsync results per
// business — no new endpoint needed, visits already carry business/operator/service names.
public class FrequentBusinessItem
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public DateTime LastVisitedAt { get; set; }
    public string LastOperatorName { get; set; } = string.Empty;
    public string LastServiceLabel { get; set; } = string.Empty;

    public string VisitCountText => VisitCount == 1 ? "1 visit" : $"{VisitCount} visits";
    public string LastVisitText => $"Last visit {LastVisitedAt:d MMM} · {LastOperatorName}";
    public string UsualText => string.IsNullOrWhiteSpace(LastServiceLabel)
        ? "Tap to view"
        : $"Usual: {LastServiceLabel}";
}
