using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Shared.Domain.Models;

// Everything the business landing already fetched, handed to the flow page it launches so the flow
// does not re-fetch the same four things the customer is looking at. Seconds old at most — the flow
// opens on a tap — and the flow still fetches for itself when it is opened from anywhere else.
public sealed record BusinessSnapshot(
    BusinessResponse Business,
    IReadOnlyList<OperatorResponse> Operators,
    IReadOnlyList<ServiceResponse> Services,
    BusinessHours Hours);
