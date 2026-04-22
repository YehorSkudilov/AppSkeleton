using System.Globalization;
using Microsoft.Maui.Controls;

namespace AppSkeleton;

public class BoolToIconConverter : IValueConverter
{
    public string FalseIcon { get; set; }
    public string TrueIcon { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? TrueIcon : FalseIcon;
        }

        return FalseIcon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}