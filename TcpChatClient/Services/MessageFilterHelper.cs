using System;
using System.Collections.Generic;
using System.Linq;
using TcpChatClient.Models;

namespace TcpChatClient.Helpers
{
    // 메시지 필터링 및 날짜 헤더 삽입 도우미 클래스
    public static class MessageFilterHelper
    {
        // 메시지 리스트에서 검색/대상자 필터 + 날짜별 헤더 추가
        public static List<object> FilterMessagesWithDateHeaders(IEnumerable<ChatMessage> messages, string keyword, string me, string partner)
        {
            var result = new List<object>();
            DateTime? lastDate = null;

            var filtered = messages
                // 1차: 송/수신자 기준으로 필터
                .Where(m =>
                    (m.Sender == partner && m.Receiver == me) || (m.Sender == me && m.Receiver == partner))
                // 2차: 키워드 포함 메시지만
                .Where(m =>
                    string.IsNullOrWhiteSpace(keyword) || (m.Message?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(m => m.Timestamp);

            // 날짜별로 헤더 삽입
            foreach (var msg in filtered)
            {
                var dateOnly = msg.Timestamp.Date;
                if (lastDate == null || lastDate.Value != dateOnly)
                {
                    result.Add(new ChatDateHeader { Date = dateOnly }); // 날짜 헤더 추가
                    lastDate = dateOnly;
                }
                result.Add(msg);    // 실제 메시지 추가
            }

            return result;
        }
    }
}
