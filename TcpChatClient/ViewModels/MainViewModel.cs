using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TcpChatClient.Helpers;
using TcpChatClient.Models;
using TcpChatClient.Services;

namespace TcpChatClient.ViewModels
{
    // 채팅 메인 화면의 ViewModel
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClientSocket _socket = new();            // 네트워크 소켓
        private readonly ChatService _chatService;                // 채팅 서비스
        private readonly ChatPacketHandler _packetHandler;        // 패킷 핸들러
        private readonly Dictionary<string, int> UnreadCounts = new(); // 안읽은 메시지 카운트
        private readonly Dictionary<string, bool> TypingUsers = new(); // 타이핑 중인 유저 표시

        private readonly System.Timers.Timer _typingStartTimer;   // 타이핑 시작 타이머
        private readonly System.Timers.Timer _typingEndTimer;     // 타이핑 종료 타이머

        private string _input;
        private string _selectedUser;
        private string _selectedFilter = "접속중";
        private string _messageSearchKeyword;
        private string _userSearchKeyword;

        public string Nickname { get; }   // 내 닉네임
        public ObservableCollection<ChatMessage> AllMessages { get; } = new();     // 전체 메시지
        public ObservableCollection<object> FilteredMessages { get; } = new();     // 필터링+날짜헤더 메시지
        public ObservableCollection<string> OnlineUsers { get; } = new();          // 현재 접속자 리스트
        public ObservableCollection<string> AllUsers { get; } = new();             // 전체 유저 리스트
        public ObservableCollection<string> FilteredUserList { get; } = new();     // 필터/검색 적용 유저리스트

        // 채팅 입력값 (타이핑 알림 트리거)
        public string Input
        {
            get => _input;
            set
            {
                _input = value;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(_selectedUser))
                {
                    _typingEndTimer.Stop();
                    _typingEndTimer.Start();

                    if (!_typingStartTimer.Enabled)
                    {
                        _typingStartTimer.Start();
                    }
                }
            }
        }

        // 현재 선택된 상대 유저
        public string SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _selectedUser = value.Split('(')[0].Trim();
                OnPropertyChanged();

                if (UnreadCounts.ContainsKey(_selectedUser))
                {
                    UnreadCounts[_selectedUser] = 0;
                    UpdateFilteredUserList();
                }

                _ = _chatService.MarkMessagesAsReadAsync(_selectedUser);  // 읽음 처리 요청
                _ = _chatService.RequestHistoryAsync(Nickname, _selectedUser); // 대화 이력 요청
                ApplyMessageSearchFilter();
                OnPropertyChanged(nameof(IsOpponentTyping));
            }
        }

        // 유저리스트 필터("전체" or "접속중")
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); UpdateFilteredUserList(); }
        }

        // 메시지 검색 키워드
        public string MessageSearchKeyword
        {
            get => _messageSearchKeyword;
            set { _messageSearchKeyword = value; OnPropertyChanged(); ApplyMessageSearchFilter(); }
        }

        // 유저 검색 키워드
        public string UserSearchKeyword
        {
            get => _userSearchKeyword;
            set { _userSearchKeyword = value; OnPropertyChanged(); UpdateFilteredUserList(); }
        }

        // 상대방 타이핑 상태
        public bool IsOpponentTyping => _selectedUser != null && TypingUsers.TryGetValue(_selectedUser, out var isTyping) && isTyping;

        // 소켓 연결 상태
        public bool IsConnected => _socket.IsConnected;

        // 채팅 커맨드 (버튼/단축키와 바인딩)
        public ICommand SendCommand { get; }
        public ICommand SendFileCommand { get; }
        public ICommand DeleteCommand { get; }

        public MainViewModel() : this("디자이너") { }

        // 뷰모델 생성자(초기화)
        public MainViewModel(string username)
        {
            Nickname = username;
            _chatService = new ChatService(_socket);

            _socket.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ClientSocket.IsConnected))
                { 
                    OnPropertyChanged(nameof(IsConnected));
                }
            };

            // 타이핑 감지 타이머 초기화 (0.5초 후 타이핑 알림)
            _typingStartTimer = new System.Timers.Timer(500);
            _typingStartTimer.Elapsed += async (_, _) =>
            {
                _typingStartTimer.Stop();
                if (!string.IsNullOrWhiteSpace(_selectedUser))
                {
                    await _socket.SendTypingAsync(_selectedUser, true);
                }
            };
            _typingStartTimer.AutoReset = false;

            // 타이핑 끝났을 때 타이머 (1.5초 후 타이핑 중지 알림)
            _typingEndTimer = new System.Timers.Timer(1500);
            _typingEndTimer.Elapsed += async (_, _) =>
            {
                _typingEndTimer.Stop();
                if (!string.IsNullOrWhiteSpace(_selectedUser))
                {
                    await _socket.SendTypingAsync(_selectedUser, false);
                }
            };
            _typingEndTimer.AutoReset = false;

            // 패킷 핸들러(DI, 콜백)
            _packetHandler = new ChatPacketHandler(
                myName: Nickname,
                updateOnlineUsers: names =>
                {
                    OnlineUsers.Clear();
                    foreach (var n in names) OnlineUsers.Add(n);
                    UpdateFilteredUserList();
                },
                updateAllUsers: entries =>
                {
                    AllUsers.Clear();
                    UnreadCounts.Clear();
                    foreach (var (name, count) in entries)
                    {
                        AllUsers.Add(name);
                        UnreadCounts[name] = count;
                    }
                    UpdateFilteredUserList();
                },
                loadHistory: packets =>
                {
                    AllMessages.Clear();
                    foreach (var p in packets)
                    {
                        AllMessages.Add(new ChatMessage
                        {
                            Id = p.Id,
                            Sender = p.Sender,
                            Receiver = p.Receiver,
                            Message = p.Type == "file" ? $"[파일] {p.FileName}" : p.Content,
                            MyName = Nickname,
                            Timestamp = p.Timestamp,
                            FileName = p.FileName,
                            Content = p.Content,
                            IsRead = p.IsRead,
                            IsDeleted = p.IsDeleted
                        });
                    }
                    ApplyMessageSearchFilter();
                },
                handleNewMessage: packet =>
                {
                    var msg = new ChatMessage
                    {
                        Id = packet.Id,
                        Sender = packet.Sender,
                        Receiver = packet.Receiver,
                        Message = packet.Type == "file" ? $"[파일 수신] {packet.FileName}" : packet.Content,
                        MyName = Nickname,
                        Timestamp = packet.Timestamp,
                        FileName = packet.FileName,
                        Content = packet.Content,
                        IsRead = packet.IsRead,
                        IsDeleted = packet.IsDeleted
                    };
                    AllMessages.Add(msg);

                    // 선택중 채팅방이면 바로 추가
                    if ((msg.Sender == _selectedUser && msg.Receiver == Nickname) || (msg.Sender == Nickname && msg.Receiver == _selectedUser))
                    {
                        RefreshFilteredMessages();
                        //FilteredMessages.Add(msg);
                    }

                    // 본인 아닌 상대방이 나한테 보낸 메시지면 안읽음 카운트 증가
                    if (msg.Receiver == Nickname && msg.Sender != Nickname)
                    {
                        bool chatOpen = _selectedUser == msg.Sender;
                        bool windowFocused = Application.Current?.MainWindow is { IsActive: true };

                        if (!chatOpen || !windowFocused)
                        {
                            if (!UnreadCounts.ContainsKey(msg.Sender))
                                UnreadCounts[msg.Sender] = 0;

                            UnreadCounts[msg.Sender]++;
                            UpdateFilteredUserList();
                        }
                    }
                },
                handleDownload: SaveDownloadToFile,
                markReadNotify: (from, to) =>
                {
                    bool changed = false;
                    foreach (var m in AllMessages.Where(m => m.Sender == to && m.Receiver == from && !m.IsRead))
                    {
                        m.IsRead = true;
                        changed = true;
                    }
                    if (changed) ApplyMessageSearchFilter();
                },
                handleDeleteNotify: (sender, receiver, timestamp, id) =>
                {
                    var target = AllMessages.FirstOrDefault(m => m.Id == id);
                    if (target != null)
                    {
                        target.Message = "삭제된 메시지입니다";
                        target.IsDeleted = true;
                        RefreshFilteredMessages();
                        OnPropertyChanged(nameof(FilteredMessages));
                    }
                },
                setTypingState: (user, isTyping) =>
                {
                    TypingUsers[user] = isTyping;
                    OnPropertyChanged(nameof(IsOpponentTyping));
                });

            _socket.PacketReceived += packet => Application.Current.Dispatcher.Invoke(() => _packetHandler.Handle(packet));
            _ = _socket.SafeConnectAsync("127.0.0.1", 9000, username);

            // 메시지 전송 커맨드
            SendCommand = new RelayCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(Input) || string.IsNullOrWhiteSpace(_selectedUser))
                {
                    MessageBox.Show("보낼 메시지와 대상 유저를 선택하세요.");
                    return;
                }
                await _chatService.SendMessageAsync(Input, _selectedUser);
                Input = string.Empty;

                _typingStartTimer.Stop();
                _typingEndTimer.Stop();
                await _socket.SendTypingAsync(_selectedUser, false);
            });

            // 파일 전송 커맨드
            SendFileCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(_selectedUser))
                {
                    _ = _chatService.SendFileAsync(dlg.FileName, _selectedUser);
                }
            });

            // 메시지 삭제 커맨드
            DeleteCommand = new RelayCommand<ChatMessage>(DeleteMessage);
        }

        // 메시지 검색 필터 적용
        private void ApplyMessageSearchFilter() => RefreshFilteredMessages();

        // 필터링 후 메시지/날짜헤더 리스트 갱신
        private void RefreshFilteredMessages()
        {
            var filtered = MessageFilterHelper.FilterMessagesWithDateHeaders(AllMessages, MessageSearchKeyword, Nickname, _selectedUser);
            FilteredMessages.Clear();
            foreach (var item in filtered)
            { 
                FilteredMessages.Add(item);
            }
        }

        // 메시지 삭제 로직 (삭제조건 체크)
        private void DeleteMessage(ChatMessage msg)
        {
            if (msg == null || msg.IsDeleted || msg.IsRead) return;
            // 삭제 요청만 서버로 전송, UI는 변경하지 않는다
            _ = _chatService.SendDeleteAsync(msg);
            // UI 반영은 handleDeleteNotify에서만
        }

        // 파일 다운로드 요청
        public async Task RequestFileDownload(string serverPath, string fileName) => await _chatService.RequestFileDownloadAsync(serverPath, fileName);

        // 파일 전송 요청
        public async Task RequestFileSendAsync(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(_selectedUser))
            {
                await _chatService.SendFileAsync(filePath, _selectedUser);
            }
        }

        // 현재 채팅방 메시지 전체 읽음 처리
        public async Task MarkMessagesAsReadIfVisible()
        {
            if (!string.IsNullOrWhiteSpace(_selectedUser))
            {
                await _chatService.MarkMessagesAsReadAsync(_selectedUser);
                if (UnreadCounts.ContainsKey(_selectedUser))
                {
                    UnreadCounts[_selectedUser] = 0;
                    UpdateFilteredUserList();
                }
            }
        }

        // 파일 다운로드 실제 저장 (복호화/예외처리 포함)
        private void SaveDownloadToFile(ChatPacket packet)
        {
            var dlg = new SaveFileDialog
            {
                FileName = packet.FileName,
                Title = "파일 저장",
                Filter = "모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] encrypted = Convert.FromBase64String(packet.Content);
                    byte[] decrypted = AesEncryption.DecryptBytes(encrypted);
                    File.WriteAllBytes(dlg.FileName, decrypted);

                    MessageBox.Show("파일 저장 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 유저리스트 필터/검색/안읽음 카운트 적용 리스트 갱신
        private void UpdateFilteredUserList()
        {
            FilteredUserList.Clear();
            var source = SelectedFilter == "전체" ? AllUsers : OnlineUsers;
            foreach (var user in source)
            {
                var cleanName = user.Split('(')[0].Trim();
                if (cleanName == Nickname) 
                { 
                    continue; 
                }
                if (!string.IsNullOrWhiteSpace(UserSearchKeyword) && !cleanName.Contains(UserSearchKeyword, StringComparison.OrdinalIgnoreCase))
                { 
                    continue; 
                }
                if (UnreadCounts.TryGetValue(cleanName, out int count) && count > 0)
                { 
                    FilteredUserList.Add($"{cleanName} ({count})"); 
                }
                else
                {
                    FilteredUserList.Add(cleanName);
                }
            }
        }

        // 바인딩용 PropertyChanged 이벤트
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
