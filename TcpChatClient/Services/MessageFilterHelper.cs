using System;
using System.Collections.Generic;
using System.Linq;
using TcpChatClient.Models;

namespace TcpChatClient.Helpers
{
    public static class MessageFilterHelper
    {
        public static List<object> FilterMessagesWithDateHeaders(IEnumerable<ChatMessage> messages, string keyword, string me, string partner)
        {
            var result = new List<object>();
            DateTime? lastDate = null;

            var filtered = messages
                .Where(m =>
                    (m.Sender == partner && m.Receiver == me) ||
                    (m.Sender == me && m.Receiver == partner))
                .Where(m =>
                    string.IsNullOrWhiteSpace(keyword) ||
                    (m.Message?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(m => m.Timestamp);

            foreach (var msg in filtered)
            {
                var dateOnly = msg.Timestamp.Date;
                if (lastDate == null || lastDate.Value != dateOnly)
                {
                    result.Add(new ChatDateHeader { Date = dateOnly });
                    lastDate = dateOnly;
                }
                result.Add(msg);
            }

            return result;
        }
    }
}
