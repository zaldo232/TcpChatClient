using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TcpChatClient.Converters
{
    // bool 값을 HorizontalAlignment로 변환하는 컨버터
    public class BoolToAlignmentConverter : IValueConverter
    {
        // true면 오른쪽 정렬, false면 왼쪽 정렬 반환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        // 역변환은 미구현 (사용하지 않음)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
