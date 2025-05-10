using System.IO;

namespace TcpChatClient.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string MyName { get; set; }

        public bool IsMine => Sender == MyName;
        public bool IsFile => !string.IsNullOrEmpty(FileName);

        public string Display => IsFile
            ? $"{Sender}: [파일] {FileName}"
            : $"{Sender}: {Message}";
    }


}
