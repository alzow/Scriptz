namespace QueueApp.Features.Profile.Helpers;

public static class ProfileHelper
{
    public const string FallbackCategoryLabel = "Business";

    public static string CategoryLabel(string category) =>
        string.IsNullOrWhiteSpace(category)
            ? FallbackCategoryLabel
            : char.ToUpperInvariant(category[0]) + category[1..];
}
