using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real OperatorService in DEBUG builds.
public class StubOperatorService : IOperatorService
{
    private readonly List<OperatorResponse> _operators = new()
    {
        new() { Id = Guid.NewGuid(), DisplayName = "Ahmed", SortOrder = 0, IsAvailable = true },
        new() { Id = Guid.NewGuid(), DisplayName = "Yusuf", SortOrder = 1, IsAvailable = true },
    };

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId)
        => Task.FromResult(_operators.OrderBy(o => o.SortOrder).ToList());
}
