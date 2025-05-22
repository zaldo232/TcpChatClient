using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TcpChatClient.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool val = value is bool b && b;

            return (invert ? !val : val) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool isVisible = value is Visibility v && v == Visibility.Visible;

            return invert ? !isVisible : isVisible;
        }
    }
}
