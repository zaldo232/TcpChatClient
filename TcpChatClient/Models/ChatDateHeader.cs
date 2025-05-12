namespace TcpChatClient.Models
{
    public class ChatDateHeader
    {
        public DateTime Date { get; set; }
        public string Display => Date.ToString("yyyy년 M월 d일 (ddd)");
    }

}
