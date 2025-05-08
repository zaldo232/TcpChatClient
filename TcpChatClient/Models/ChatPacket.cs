namespace TcpChatClient.Models
{
    public class ChatPacket
    {
        public string Type { get; set; } // "message" or "file"
        public string Sender { get; set; }
        public string Content { get; set; } // 텍스트 또는 Base64 파일 데이터
        public string FileName { get; set; } // 파일 전송 시
    }

}
