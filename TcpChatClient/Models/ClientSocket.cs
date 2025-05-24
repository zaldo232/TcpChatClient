using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TcpChatClient.Helpers;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace TcpChatClient.Models
{
    public class ClientSocket : INotifyPropertyChanged
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public string Nickname { get; set; }

        public event Action<ChatPacket> PacketReceived;

        private System.Timers.Timer _pingTimer;
        private DateTime _lastPongReceived = DateTime.Now;

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task SafeConnectAsync(string ip, int port, string nickname)
        {
            Nickname = nickname;

            try
            {
                _pingTimer?.Stop();         // 기존 ping 타이머 중단
                _pingTimer?.Dispose();      // 리소스 정리
                _stream?.Dispose();
                _client?.Close();
            }
            catch { }

            int retry = 0;

            while (!IsConnected && retry++ < 5)
            {
                try
                {
                    await ConnectAsync(ip, port);
                    Debug.WriteLine("[연결 성공]");
                    StartPingTimer();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[연결 실패 {retry}/5] 재시도 중... {ex.Message}");
                    await Task.Delay(3000);
                }
            }

            Debug.WriteLine("[연결 실패. 수동 조치 필요]");
        }

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            IsConnected = true;

            StartReceiveLoop();

            await SendPacketAsync(new ChatPacket
            {
                Type = "login",
                Sender = Nickname,
                Content = $"{Nickname} 접속"
            });
        }


        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            Nickname = nickname;
            await ConnectAsync(ip, port);
        }


        private void StartPingTimer()
        {
            _pingTimer = new System.Timers.Timer(10000); // 10초
            _pingTimer.Elapsed += async (_, _) =>
            {
                try
                {
                    await SendPacketAsync(new ChatPacket { Type = "ping", Sender = Nickname });

                    if ((DateTime.Now - _lastPongReceived).TotalSeconds > 30)
                    {
                        Debug.WriteLine("[Ping 응답 없음 → 재연결 시도]");
                        IsConnected = false;
                        _pingTimer.Stop();
                        await SafeConnectAsync("127.0.0.1", 9000, Nickname);
                    }
                }
                catch
                {
                    Debug.WriteLine("[Ping 전송 실패]");
                    IsConnected = false;
                }
            };
            _pingTimer.Start();
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
            if (_stream == null || !_client.Connected)
            {
                Debug.WriteLine("[패킷 전송 실패: 연결 안 됨]");
                return;
            }

            if (packet.Type == "message" && !string.IsNullOrWhiteSpace(packet.Content))
            {
                Debug.WriteLine($"[전송 전 평문] {packet.Content}");
                string encrypted = AesEncryption.Encrypt(packet.Content);
                Debug.WriteLine($"[전송 후 암호문] {encrypted}");
                if (!string.IsNullOrWhiteSpace(encrypted))
                    packet.Content = encrypted;
                else
                    return;
            }

            var json = JsonSerializer.Serialize(packet);
            var data = Encoding.UTF8.GetBytes(json + "\n");

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[패킷 전송 중 오류] {ex.Message}");
                IsConnected = false;
            }
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
                    {
                        if (packet.Type == "pong")
                        {
                            _lastPongReceived = DateTime.Now;
                            IsConnected = true;
                            continue;
                        }

                        PacketReceived?.Invoke(packet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[수신 오류] {ex.Message}");
                    break;
                }
            }

            IsConnected = false;
            Debug.WriteLine("[수신 루프 종료]");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
