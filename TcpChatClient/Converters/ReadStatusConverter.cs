using System;
using System.Globalization;
using System.Windows.Data;

namespace TcpChatClient.Converters
{
    // bool 값을 읽음, 안읽음 문자열로 변환하는 컨버터
    public class ReadStatusConverter : IValueConverter
    {
        // true면 읽음, false면 안읽음 반환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "읽음" : "안읽음";
        }

        // 역변환 미구현 (사용하지 않음)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}