﻿namespace TcpChatClient.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string Display => $"{Sender}: {Message}";
    }
}
