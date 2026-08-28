using System.Globalization;
using System.Linq;

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

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colors)
        {
            var colorPair = colors.Split('|');
            if (colorPair.Length == 2)
            {
                return boolValue ? Color.FromArgb(colorPair[0]) : Color.FromArgb(colorPair[1]);
            }
        }
        return Colors.Transparent;
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

public class IsNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null;
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

// Compares the two bound values (item, currently-selected item) for equality and returns one of two
// colors — used to highlight the selected item in a picker CollectionView/BindableLayout.
public class ItemEqualsSelectedToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [var item, var selected, ..] && parameter is string colors)
        {
            var colorPair = colors.Split('|');
            if (colorPair.Length == 2)
            {
                var isSelected = item is not null && Equals(item, selected);
                return Color.FromArgb(isSelected ? colorPair[0] : colorPair[1]);
            }
        }
        return Colors.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
// to a color, keyed off a "|"-separated hex-color parameter matching that same order (6 entries).
public class WaitBucketToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string bucket || parameter is not string hexColors)
            return Colors.Transparent;

        var parts = hexColors.Split('|');
        if (parts.Length != 6)
            return Colors.Transparent;

        var index = bucket switch
        {
            "go" => 0,
            "wait" => 1,
            "busy" => 2,
            "book" => 3,
            "off" => 4,
            _ => 5, // "unknown"
        };

        return Color.FromArgb(parts[index]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
