using System.Collections.ObjectModel;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.OperatorQueue;

public class OperatorColumn
{
    public OperatorResponse Operator { get; set; } = new();
    public ObservableCollection<QueueEntryResponse> Waiting { get; } = new();
    public bool IsAddingWalkIn { get; set; }
}
