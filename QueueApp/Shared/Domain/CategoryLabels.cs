namespace QueueApp.Shared.Domain;

// One business's whole vocabulary. "In the chair" is a barber's phrase and a doctor's mistake, so
// nothing that changes with the trade is written into a screen — a screen asks this for it. Adding
// a category means adding a row here, and the derived forms below mean a row is six words, not
// twelve.
public sealed record CategoryLabelSet(
    string StepHeading,
    string Noun,
    string PluralNoun,
    string SectionTitle,
    string VenueNoun,
    string ServingPhrase)
{
    private const string SinceSuffix = "SINCE";
    private const string NowSuffix = "now";

    public string LowerNoun => Noun.ToLowerInvariant();

    public string Venue => $"the {VenueNoun}";
    public string VenueCapitalised => $"The {VenueNoun}";
    public string VenuePossessive => $"the {VenueNoun}'s";

    // The four shapes the serving phrase is asked for: a status pill, a hero caption, a board
    // caption and a fact-row label.
    public string ServingStatus => ServingPhrase.ToUpperInvariant();
    public string ServingSinceCaption => $"{ServingStatus} {SinceSuffix}";
    public string ServingNowText => $"{ServingPhrase} {NowSuffix}";
    public string ServingLabel => ServingPhrase.Length == 0
        ? string.Empty
        : string.Concat(char.ToUpperInvariant(ServingPhrase[0]), ServingPhrase[1..]);
}

public static class CategoryLabels
{
    private static readonly CategoryLabelSet Fallback =
        new("Who's helping you?", "Staff", "staff", "Team", "business", "being served");

    // Keyed by every key in CategoryCatalog, so no live category resolves to the fallback by
    // accident — the fallback is for a category the API grows before the app is rebuilt.
    private static readonly Dictionary<string, CategoryLabelSet> ByCategory = new()
    {
        ["barber"] = new("Who's cutting?", "Barber", "barbers", "Team", "shop", "in the chair"),
        ["hairsalon"] = new("Who's cutting?", "Stylist", "stylists", "Team", "salon", "in the chair"),
        ["nails"] = new("Who's helping you?", "Therapist", "therapists", "Team", "salon", "in the chair"),
        ["spa"] = new("Who's helping you?", "Therapist", "therapists", "Team", "spa", "in the room"),
        ["carwash"] = new("Which bay?", "Bay", "bays", "Bays", "wash", "in the bay"),
        ["carservice"] = new("Which bay?", "Bay", "bays", "Bays", "workshop", "in the bay"),
        ["tyre"] = new("Which bay?", "Bay", "bays", "Bays", "fitment centre", "in the bay"),
        ["doctor"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners", "practice", "in the room"),
        ["dentist"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners", "practice", "in the chair"),
        ["vet"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners", "practice", "in the room"),
        ["optometry"] = new("Who are you seeing?", "Practitioner", "practitioners", "Practitioners", "practice", "in the room"),
        ["pharmacy"] = new("Who's helping you?", "Pharmacist", "pharmacists", "Team", "pharmacy", "at the counter"),
        ["restaurant"] = new("Who's serving?", "Server", "servers", "Team", "restaurant", "at the table"),
        ["laundry"] = new("Who's helping you?", "Staff", "staff", "Team", "shop", "being served"),
        ["licensing"] = new("Which counter?", "Counter", "counters", "Counters", "office", "at the counter"),
        ["phonerepair"] = new("Who's helping you?", "Technician", "technicians", "Technicians", "shop", "being repaired"),
        ["other"] = Fallback,
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
