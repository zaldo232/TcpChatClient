using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TcpChatClient.Models
{
    public class ClientSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public string Nickname { get; set; }

        public event Action<ChatPacket> PacketReceived;

        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            Nickname = nickname;
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            StartReceiveLoop();

            await SendPacketAsync(new ChatPacket
            {
                Type = "login",
                Sender = Nickname,
                Content = $"{Nickname} 접속"
            });
        }

        public async Task SendMessageAsync(string message, string receiver)
        {
            var packet = new ChatPacket
            {
                Type = "message",
                Sender = Nickname,
                Receiver = receiver,
                Content = message
            };
            await SendPacketAsync(packet);
        }

        public async Task SendFileAsync(string filePath, string receiver)
        {
            var bytes = File.ReadAllBytes(filePath);
            var packet = new ChatPacket
            {
                Type = "file",
                Sender = Nickname,
                Receiver = receiver,
                FileName = Path.GetFileName(filePath),
                Content = Convert.ToBase64String(bytes)
            };
            await SendPacketAsync(packet);
        }

        public async Task MarkMessagesAsReadAsync(string withUser)
        {
            var packet = new ChatPacket
            {
                Type = "mark_read",
                Sender = Nickname,
                Receiver = withUser
            };
            await SendPacketAsync(packet);
        }

        public async Task SendPacketAsync(ChatPacket packet)
        {
            var json = JsonSerializer.Serialize(packet);
            var data = Encoding.UTF8.GetBytes(json + "\n");
            await _stream.WriteAsync(data, 0, data.Length);
        }

        private async void StartReceiveLoop()
        {
            using var reader = new StreamReader(_stream, Encoding.UTF8);
            while (true)
            {
                try
                {
                    string json = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(json)) break;

                    var packet = JsonSerializer.Deserialize<ChatPacket>(json);
                    if (packet != null)
                        PacketReceived?.Invoke(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[수신 오류] {ex.Message}");
                    break;
                }
            }
        }
    }
}
