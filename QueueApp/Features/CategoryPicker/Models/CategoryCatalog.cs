namespace QueueApp.Features.CategoryPicker.Models;

public record ServiceCategory(string Key, string Display, string IconSource, bool Available);

public static class CategoryCatalog
{
    public static readonly IReadOnlyList<ServiceCategory> All = new List<ServiceCategory>
    {
        new("barber",     "Barbers",           "ic_barbershop",  true),
        new("hairsalon",  "Hair salons",       "ic_hair_salon",  true),
        new("nails",      "Nails & beauty",    "ic_nails",       true),
        new("spa",        "Spa & wellness",    "ic_spa",         true),
        new("carwash",    "Car washes",        "ic_car_wash",    true),
        new("carservice", "Car service",       "ic_car_service", true),
        new("tyre",       "Tyre & fitment",    "ic_tyre",        true),
        new("doctor",     "Doctors & clinics", "ic_doctor",      true),
        new("dentist",    "Dentists",          "ic_dentist",     true),
        new("optometry",  "Optometrists",      "ic_optometry",   true),
        new("pharmacy",   "Pharmacies",        "ic_pharmacy",    true),
        new("vet",        "Veterinary",        "ic_vet",         true),
        new("restaurant", "Restaurants",       "ic_restaurant",  true),
        new("laundry",    "Laundry",           "ic_laundry",     true),
        new("licensing",  "Licensing offices", "ic_licensing",   true),
        new("phonerepair","Device repair",     "ic_phone_repair",true),
        new("other",      "More services",     "ic_other",       true),
    };
}
