namespace QueueApp.Features.CategoryPicker;

public record ServiceCategory(string Key, string Display, string Icon, bool Available);

public static class CategoryCatalog
{
    public static readonly IReadOnlyList<ServiceCategory> All = new List<ServiceCategory>
    {
        new("barber",  "Barbers",    "✂",  true),
        new("carwash", "Car washes", "\U0001F697", false),
    };
}
