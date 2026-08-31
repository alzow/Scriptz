using System.Globalization;
using System.Linq;
using QueueApp.Framework.Theming;

namespace QueueApp.Converters;

public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string texts)
        {
            var textPair = texts.Split('|');
            if (textPair.Length == 2)
            {
                return boolValue ? textPair[0] : textPair[1];
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? 1.0 : 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ActiveToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? "Deactivate" : "Reactivate";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Compares the two bound values (item, currently-selected item) for equality and returns a bool —
// used to drive scale/animation triggers for the selected item in a picker CollectionView.
public class ItemEqualsSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values is [var item, var selected, ..] && item is not null && Equals(item, selected);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Full opacity when nothing is selected, or when this item is the selection; dimmed otherwise —
// used to fade out the unselected siblings once a pick is made.
public class ItemSelectionOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [var item, var selected, ..])
            return 1.0;

        return selected is null || Equals(item, selected) ? 1.0 : 0.4;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EdgeItemToMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double extra = 16;
        if (parameter is string p && double.TryParse(p, out var parsed))
            extra = parsed;

        if (values is [var item, System.Collections.IEnumerable items, ..])
        {
            var list = items.Cast<object>().ToList();
            var isFirst = list.Count > 0 && Equals(list[0], item);
            var isLast = list.Count > 0 && Equals(list[^1], item);
            return new Thickness(isFirst ? extra : 0, 0, isLast ? extra : 0, 0);
        }

        return new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Maps a BrowseBusinessSummaryResponse.WaitBucket string ("go"|"wait"|"busy"|"book"|"off"|"unknown")
// to a themed token.
//
// This used to take a "|"-separated list of hex colours as a ConverterParameter, which put six
// literal colours into the page and could not survive a theme switch — three of them were alpha
// tints, which composite differently over a card than over the page and vanish over a light one.
// The mapping lives here now and resolves through ThemePalette, so the page names a role and not
// a colour.
public abstract class WaitBucketConverterBase : IValueConverter
{
    protected abstract string GoToken { get; }
    protected abstract string WaitToken { get; }
    protected abstract string BusyToken { get; }
    protected abstract string NeutralToken { get; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var token = (value as string) switch
        {
            "go" => GoToken,
            "wait" => WaitToken,
            "busy" => BusyToken,
            // "book", "off" and anything unrecognised are not a wait state, so they stay neutral.
            _ => NeutralToken,
        };

        var color = ThemePalette.Get(token);

        // Border.Stroke is a Brush, every other consumer wants the Color itself.
        return targetType == typeof(Brush) ? new SolidColorBrush(color) : color;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// The tile behind a category icon: a solid tint, never alpha.
public sealed class WaitBucketToTintConverter : WaitBucketConverterBase
{
    protected override string GoToken => "AccentTint";
    protected override string WaitToken => "PurpleTint";
    protected override string BusyToken => "DangerTint";
    protected override string NeutralToken => "Raised";
}

// That tile's 1px edge. A thin stroke takes the text variant — the vivid green is 1.22:1 on light.
public sealed class WaitBucketToStrokeConverter : WaitBucketConverterBase
{
    protected override string GoToken => "AccentText";
    protected override string WaitToken => "PurpleText";
    protected override string BusyToken => "DangerText";
    protected override string NeutralToken => "Border";
}

// The wait bar itself is a fill, so it keeps the vivid brand colour in both themes.
public sealed class WaitBucketToBarConverter : WaitBucketConverterBase
{
    protected override string GoToken => "Accent";
    protected override string WaitToken => "Purple";
    protected override string BusyToken => "Danger";
    protected override string NeutralToken => "Border";
}
