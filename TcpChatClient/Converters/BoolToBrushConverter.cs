using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TcpChatClient.Converters
{
    // bool 값을 Brush(색상)로 변환하는 컨버터
    public class BoolToBrushConverter : IValueConverter
    {
        // true면 연노랑, false면 연파랑 반환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.LightYellow : Brushes.LightBlue;
        }

        // 역변환 미구현 (사용하지 않음)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
