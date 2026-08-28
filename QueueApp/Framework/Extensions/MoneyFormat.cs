using System.Globalization;

namespace QueueApp.Framework.Extensions;

// Rands, grouped with a space rather than a comma — "R5 700", the way a South African price is
// written on a board. ServiceResponse.PriceDisplay predates this and formats without grouping;
// it's left alone so nothing outside the agenda shifts.
public static class MoneyFormat
{
    private static readonly NumberFormatInfo Rand = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ".",
        NumberGroupSizes = new[] { 3 },
    };

    public static string Format(int? priceCents)
    {
        if (priceCents is null)
            return "";

        var rands = priceCents.Value / 100m;

        // Whole rands read cleaner on a row at arm's length; cents only appear when they exist.
        return rands == decimal.Truncate(rands)
            ? "R" + rands.ToString("#,##0", Rand)
            : "R" + rands.ToString("#,##0.00", Rand);
    }
}
