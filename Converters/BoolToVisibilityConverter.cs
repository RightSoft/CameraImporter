using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CameraImporter.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Boolean.TryParse(value.ToString(), out bool parsedValue);
                return parsedValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Enum.TryParse(value.ToString(), out Visibility parsedVisibility);
                return parsedVisibility == Visibility.Visible;
            }

            return false;
        }
    }

    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Boolean.TryParse(value.ToString(), out bool parsedValue);
                return !parsedValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                Enum.TryParse(value.ToString(), out Visibility parsedVisibility);
                return parsedVisibility != Visibility.Visible;
            }

            return true;
        }
    }
}
