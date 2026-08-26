namespace QueueApp.Features.BusinessDetail.Flow;

public sealed record CategoryLabelSet(string StepHeading, string Noun, string PluralNoun, string SectionTitle);

// "Who's cutting?" is wrong for a car wash. The operator step heading, the picker noun and the
// landing's team section title all come from the business category. Categories get added, so the
// fallback is the point of this rather than an exhaustive table.
public static class CategoryLabels
{
    private static readonly CategoryLabelSet Fallback = new("Who's helping you?", "Staff", "staff", "Team");

    private static readonly Dictionary<string, CategoryLabelSet> ByCategory = new()
    {
        ["barber"] = new("Who's cutting?", "Barber", "barbers", "Team"),
        ["hairsalon"] = new("Who's cutting?", "Barber", "barbers", "Team"),
        ["nails"] = new("Who's helping you?", "Therapist", "therapists", "Team"),
        ["spa"] = new("Who's helping you?", "Therapist", "therapists", "Team"),
        ["carwash"] = new("Which bay?", "Bay", "bays", "Bays"),
        ["carservice"] = new("Which bay?", "Bay", "bays", "Bays"),
        ["tyre"] = new("Which bay?", "Bay", "bays", "Bays"),
        ["doctor"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners"),
        ["dentist"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners"),
        ["vet"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners"),
        ["optometry"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners"),
    };

    // business_category's enum labels were never captured into the verified schema (§1g), so match
    // on a normalised key — "car_wash" and "carwash" resolve the same either way.
    public static CategoryLabelSet Resolve(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Fallback;

        var key = category.Replace("_", "").Replace(" ", "").ToLowerInvariant();
        return ByCategory.TryGetValue(key, out var labels) ? labels : Fallback;
    }
}
