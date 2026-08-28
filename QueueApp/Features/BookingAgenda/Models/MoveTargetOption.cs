using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class MoveTargetOption
{
    public required OperatorResponse Operator { get; init; }
    public string Label => $"Move to {Operator.DisplayName}";
}
