namespace QueueApp.Features.CategoryPicker;

public record ServiceCategory(string Key, string Display, string IconSource, bool Available);

public static class CategoryCatalog
{
    public static readonly IReadOnlyList<ServiceCategory> All = new List<ServiceCategory>
    {
        new("barber",     "Barbers",           "ic_barbershop",  true),
        new("hairsalon",  "Hair salons",       "ic_hair_salon",  false),
        new("nails",      "Nails & beauty",    "ic_nails",       false),
        new("spa",        "Spa & wellness",    "ic_spa",         false),
        new("carwash",    "Car washes",        "ic_car_wash",    false),
        new("carservice", "Car service",       "ic_car_service", false),
        new("tyre",       "Tyre & fitment",    "ic_tyre",        false),
        new("doctor",     "Doctors & clinics", "ic_doctor",      false),
        new("dentist",    "Dentists",          "ic_dentist",     false),
        new("optometry",  "Optometrists",      "ic_optometry",   false),
        new("pharmacy",   "Pharmacies",        "ic_pharmacy",    false),
        new("vet",        "Veterinary",        "ic_vet",         false),
        new("restaurant", "Restaurants",       "ic_restaurant",  false),
        new("laundry",    "Laundry",           "ic_laundry",     false),
        new("licensing",  "Licensing offices", "ic_licensing",   false),
        new("phonerepair","Device repair",     "ic_phone_repair",false),
        new("other",      "More services",     "ic_other",       false),
    };
}
