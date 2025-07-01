using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using TcpChatClient.Helpers;

namespace TcpChatClient.Models
{
    // TCP 채팅 클라이언트 소켓 관리 클래스 (INotifyPropertyChanged 구현)
    public class ClientSocket : INotifyPropertyChanged
    {
        private TcpClient _client;            // 실제 TCP 클라이언트
        private NetworkStream _stream;        // 네트워크 스트림
        public string Nickname { get; set; }  // 내 닉네임

        public event Action<ChatPacket> PacketReceived; // 패킷 수신 시 호출되는 이벤트

        private System.Timers.Timer _pingTimer;          // Ping/Pong 연결 체크용 타이머
        private DateTime _lastPongReceived = DateTime.Now; // 마지막 Pong 응답 시각

        // 연결 상태 관리 (바인딩/알림)
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

        // 안전한 서버 연결 및 재시도 로직 (최대 5회)
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

        // 실제 서버 연결 및 네트워크 스트림 오픈
        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            IsConnected = true;

            StartReceiveLoop(); // 패킷 수신 루프 시작

            await SendPacketAsync(new ChatPacket
            {
                Type = "login",
                Sender = Nickname,
                Content = $"{Nickname} 접속"
            });
        }

        // 닉네임까지 포함해 바로 연결
        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            Nickname = nickname;
            await ConnectAsync(ip, port);
        }

        // Ping/Pong 타이머 시작(10초마다 Ping, 30초 응답없으면 재연결)
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

        // 일반 메시지 전송
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

        // 파일 전송 (파일 읽어 암호화 후 Base64 인코딩)
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

        // 서버 파일 다운로드 요청
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

        // 상대와의 메시지 전체 읽음 처리 요청
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

        // 패킷을 서버로 전송 (메시지면 암호화)
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

        // 타이핑 이벤트 전송
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

        // 서버로부터 패킷 수신(루프)
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

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
