namespace TcpChatClient.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public string Display => $"{Sender}: {Message}";

        public string MyName { get; set; } // ViewModel에서 지정
        public bool IsMine => Sender == MyName;
    }
}
