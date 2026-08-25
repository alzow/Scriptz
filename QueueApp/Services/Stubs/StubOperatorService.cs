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

    private readonly List<OperatorAvailabilityResponse> _availability = new();

    public Task<List<OperatorAvailabilityResponse>> GetAvailabilityAsync(Guid operatorId)
        => Task.FromResult(_availability
            .Where(a => a.OperatorId == operatorId)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .ToList());

    public Task<List<OperatorAvailabilityResponse>> CreateAvailabilityAsync(CreateAvailabilityRequest request)
    {
        var window = new OperatorAvailabilityResponse
        {
            Id = Guid.NewGuid(),
            OperatorId = request.OperatorId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
        };
        _availability.Add(window);
        return Task.FromResult(new List<OperatorAvailabilityResponse> { window });
    }

    public Task DeleteAvailabilityAsync(Guid id)
    {
        _availability.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }

    private readonly List<AvailabilityBlockResponse> _availabilityBlocks = new();

    public Task<List<AvailabilityBlockResponse>> GetAvailabilityBlocksAsync(Guid operatorId)
        => Task.FromResult(_availabilityBlocks
            .Where(b => b.OperatorId == operatorId)
            .OrderBy(b => b.StartsAt)
            .ToList());

    public Task<List<AvailabilityBlockResponse>> CreateAvailabilityBlockAsync(CreateAvailabilityBlockRequest request)
    {
        var block = new AvailabilityBlockResponse
        {
            Id = Guid.NewGuid(),
            OperatorId = request.OperatorId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Reason = request.Reason,
        };
        _availabilityBlocks.Add(block);
        return Task.FromResult(new List<AvailabilityBlockResponse> { block });
    }

    public Task DeleteAvailabilityBlockAsync(Guid id)
    {
        _availabilityBlocks.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }
}
