using System.Windows;
using System.Windows.Controls;
using TcpChatClient.Models;

namespace TcpChatClient.Helpers
{
    public class FileMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FileTemplate { get; set; }
        public DataTemplate? TextTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessage msg)
                return msg.IsFile ? FileTemplate : TextTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
