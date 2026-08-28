using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Shared.Domain.Models;

public sealed class ServiceChoiceItem : ObservableObject
{
    public required ServiceResponse Service { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DurationText { get; init; } = string.Empty;
    public string PriceText { get; init; } = string.Empty;
    public bool IsSelected { get; set; }

    public static ServiceChoiceItem From(ServiceResponse service) => new()
    {
        Service = service,
        Name = service.Name,
        DurationText = $"{service.EstMinutes} min",
        PriceText = service.PriceDisplay,
    };
}
