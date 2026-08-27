using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class AddBookingServiceOption : ObservableObject
{
    public required ServiceResponse Service { get; init; }

    public string Name => Service.Name;
    public string MetaText { get; init; } = string.Empty;
    public bool IsSelected { get; set; }

    public static AddBookingServiceOption From(ServiceResponse service)
    {
        var price = service.PriceCents is null ? "no price" : MoneyFormat.Format(service.PriceCents);

        return new AddBookingServiceOption
        {
            Service = service,
            MetaText = $"{service.EstMinutes} min · {price}",
        };
    }
}
