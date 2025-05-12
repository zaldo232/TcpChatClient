using System.Windows;
using System.Windows.Controls;
using TcpChatClient.Models;

namespace TcpChatClient.Helpers
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate DateTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                ChatDateHeader => DateTemplate,
                ChatMessage m => m.IsFile ? FileTemplate : TextTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
