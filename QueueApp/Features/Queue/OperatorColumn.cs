using System.Collections.ObjectModel;
using ScriptzApp.Services.Api.Queue.Models;

namespace ScriptzApp.Features.Queue;

public class OperatorColumn
{
    public OperatorResponse Operator { get; set; } = new();
    public ObservableCollection<QueueEntryResponse> Waiting { get; } = new();
}
