namespace QueueApp.Features.CategoryPicker;

public record ServiceCategory(string Key, string Display, string IconSource, bool Available);

public static class CategoryCatalog
{
    public static readonly IReadOnlyList<ServiceCategory> All = new List<ServiceCategory>
    {
        new("barber",  "Barbers",    "ic_barbershop", true),
        new("carwash", "Car washes", "ic_car_wash",   false),
    };
}
