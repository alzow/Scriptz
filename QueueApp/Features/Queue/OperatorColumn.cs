using System.Collections.ObjectModel;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.Queue;

public class OperatorColumn
{
    public OperatorResponse Operator { get; set; } = new();
    public ObservableCollection<QueueEntryResponse> Waiting { get; } = new();
}
