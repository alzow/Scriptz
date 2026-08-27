using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed class AddBookingServiceOption : ObservableObject
{
    public required ServiceResponse Service { get; init; }
    public required bool Fits { get; init; }

    public string Name => Service.Name;
    public string MetaText { get; init; } = string.Empty;
    public double Opacity => Fits ? 1 : 0.38;
    public bool IsSelected { get; set; }

    public static AddBookingServiceOption From(ServiceResponse service, int windowMinutes)
    {
        var fits = service.EstMinutes <= windowMinutes;
        var price = service.PriceCents is null ? "no price" : MoneyFormat.Format(service.PriceCents);

        return new AddBookingServiceOption
        {
            Service = service,
            Fits = fits,
            MetaText = fits
                ? $"{service.EstMinutes} min · {price}"
                : $"{service.EstMinutes} min · won't fit",
        };
    }
}
