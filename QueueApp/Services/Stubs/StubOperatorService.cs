using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real OperatorService in DEBUG builds.
public class StubOperatorService : IOperatorService
{
    private readonly List<OperatorResponse> _operators = new()
    {
        new() { Id = Guid.NewGuid(), DisplayName = "Ahmed", SortOrder = 0, IsAvailable = true, IsActive = true },
        new() { Id = Guid.NewGuid(), DisplayName = "Yusuf", SortOrder = 1, IsAvailable = true, IsActive = true },
    };

    public Task<List<OperatorResponse>> GetOperatorsAsync(Guid businessId)
        => Task.FromResult(_operators.Where(o => o.IsActive).OrderBy(o => o.SortOrder).ToList());

    public Task<List<OperatorResponse>> GetAllOperatorsForManagementAsync(Guid businessId)
        => Task.FromResult(_operators.OrderBy(o => o.SortOrder).ToList());

    public Task<List<OperatorResponse>> CreateOperatorAsync(CreateOperatorRequest request)
    {
        var op = new OperatorResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            DisplayName = request.DisplayName,
            SortOrder = request.SortOrder,
            IsAvailable = true,
            IsActive = true,
        };
        _operators.Add(op);
        return Task.FromResult(new List<OperatorResponse> { op });
    }

    public Task UpdateOperatorAsync(Guid id, UpdateOperatorRequest request)
    {
        var op = _operators.FirstOrDefault(o => o.Id == id);
        if (op != null)
        {
            op.DisplayName = request.DisplayName;
            op.SortOrder = request.SortOrder;
        }
        return Task.CompletedTask;
    }

    public Task SetOperatorActiveAsync(Guid id, bool isActive)
    {
        var op = _operators.FirstOrDefault(o => o.Id == id);
        if (op != null) op.IsActive = isActive;
        return Task.CompletedTask;
    }
}
