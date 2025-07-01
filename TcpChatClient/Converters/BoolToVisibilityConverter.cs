using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TcpChatClient.Converters
{
    // bool 값을 WPF Visibility로 변환하는 컨버터
    public class BoolToVisibilityConverter : IValueConverter
    {
        // true면 Visible, false면 Collapsed 반환 (파라미터에 invert 넣으면 반대)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool val = value is bool b && b;

            return (invert ? !val : val) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Visibility -> bool 역변환 ("invert" 파라미터 지원)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "invert";
            bool isVisible = value is Visibility v && v == Visibility.Visible;

            return invert ? !isVisible : isVisible;
        }
    }
}
