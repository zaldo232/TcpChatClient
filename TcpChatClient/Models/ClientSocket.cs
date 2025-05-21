using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TcpChatClient.Helpers;
using System.Diagnostics;

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
            string fileName = Path.GetFileName(filePath);
            byte[] fileBytes = File.ReadAllBytes(filePath);
            byte[] encryptedBytes = AesEncryption.EncryptBytes(fileBytes);
            string base64 = Convert.ToBase64String(encryptedBytes);

            var packet = new ChatPacket
            {
                Type = "file",
                Sender = Nickname,
                Receiver = receiver,
                FileName = fileName,
                Content = base64
            };
            await SendPacketAsync(packet);
        }

        public async Task SendDownloadRequestAsync(string serverPath, string fileName)
        {
            var packet = new ChatPacket
            {
                Type = "download",
                Sender = Nickname,
                Content = serverPath,
                FileName = fileName
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
            if (packet.Type == "message" && !string.IsNullOrWhiteSpace(packet.Content))
            {
                Debug.WriteLine($"[전송 전 평문] {packet.Content}");

                string encrypted = AesEncryption.Encrypt(packet.Content);
                Debug.WriteLine($"[전송 후 암호문] {encrypted}");

                if (!string.IsNullOrWhiteSpace(encrypted))
                    packet.Content = encrypted;
                else
                    return; // 암호화 실패 시 전송 안 함
            }

            var json = JsonSerializer.Serialize(packet);
            var data = Encoding.UTF8.GetBytes(json + "\n");
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public async Task SendTypingAsync(string receiver, bool isTyping)
        {
            var packet = new ChatPacket
            {
                Type = "typing",
                Sender = Nickname,
                Receiver = receiver,
                Content = isTyping ? "start" : "stop"
            };

            await SendPacketAsync(packet);
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
