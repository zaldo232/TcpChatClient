using System.Windows;
using System.Windows.Controls;
using TcpChatClient.Models;

namespace TcpChatClient.Helpers
{
    // 채팅 메시지 유형에 따라 DataTemplate을 선택하는 셀렉터
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }   // 일반 텍스트 메시지용 템플릿
        public DataTemplate FileTemplate { get; set; }   // 파일 메시지용 템플릿
        public DataTemplate DateTemplate { get; set; }   // 날짜 구분 헤더용 템플릿

        // 아이템 타입에 따라 알맞은 템플릿 반환
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                ChatDateHeader => DateTemplate,           // 날짜 헤더
                ChatMessage m => m.IsFile ? FileTemplate : TextTemplate,    // 파일/텍스트 메시지 구분
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
