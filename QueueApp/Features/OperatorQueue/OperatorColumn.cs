using System.Collections.ObjectModel;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.OperatorQueue;

public class OperatorColumn
{
    public OperatorResponse Operator { get; set; } = new();
    public QueueEntryResponse? Serving { get; set; }
    public ObservableCollection<QueueEntryResponse> Waiting { get; } = new();
}
