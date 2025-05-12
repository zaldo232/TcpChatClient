using System;

namespace TcpChatClient.Models
{
    public class ChatPacket
    {
        public string Type { get; set; }         // "message", "file", "userlist", "history", etc.
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }      // 텍스트 또는 Base64
        public string FileName { get; set; }     // 파일일 때만 사용
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
    }
}
