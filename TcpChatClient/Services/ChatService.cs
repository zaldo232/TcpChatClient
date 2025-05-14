using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TcpChatClient.Models;

namespace TcpChatClient.Services
{
    public class ChatService
    {
        private readonly ClientSocket _socket;

        public ChatService(ClientSocket socket)
        {
            _socket = socket;
        }

        public Task ConnectAsync(string nickname)
            => _socket.ConnectAsync("127.0.0.1", 9000, nickname);

        public Task SendMessageAsync(string text, string receiver)
            => _socket.SendMessageAsync(text, receiver);

        public Task SendFileAsync(string filePath, string receiver)
            => _socket.SendFileAsync(filePath, receiver);

        public Task MarkMessagesAsReadAsync(string withUser)
            => _socket.MarkMessagesAsReadAsync(withUser);

        public Task RequestFileDownloadAsync(string serverPath, string fileName)
            => _socket.SendDownloadRequestAsync(serverPath, fileName);

        public Task RequestHistoryAsync(string from, string to)
            => _socket.SendPacketAsync(new ChatPacket
            {
                Type = "get_history",
                Sender = from,
                Receiver = to
            });
    }
}
