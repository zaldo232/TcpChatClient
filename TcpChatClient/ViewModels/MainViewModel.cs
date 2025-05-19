using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TcpChatClient.Helpers;
using TcpChatClient.Models;
using TcpChatClient.Services;

namespace TcpChatClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClientSocket _socket = new();
        private readonly ChatService _chatService;
        private readonly ChatPacketHandler _packetHandler;
        private readonly Dictionary<string, int> UnreadCounts = new();
        private readonly Dictionary<string, bool> TypingUsers = new();

        private readonly System.Timers.Timer _typingStartTimer;
        private readonly System.Timers.Timer _typingEndTimer;

        private string _input;
        private string _selectedUser;
        private string _selectedFilter = "접속중";
        private string _messageSearchKeyword;
        private string _userSearchKeyword;

        public string Nickname { get; }
        public ObservableCollection<ChatMessage> AllMessages { get; } = new();
        public ObservableCollection<object> FilteredMessages { get; } = new();
        public ObservableCollection<string> OnlineUsers { get; } = new();
        public ObservableCollection<string> AllUsers { get; } = new();
        public ObservableCollection<string> FilteredUserList { get; } = new();

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

                _ = _chatService.MarkMessagesAsReadAsync(_selectedUser);
                _ = _chatService.RequestHistoryAsync(Nickname, _selectedUser);
                ApplyMessageSearchFilter();
                OnPropertyChanged(nameof(IsOpponentTyping));
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); UpdateFilteredUserList(); }
        }

        public string MessageSearchKeyword
        {
            get => _messageSearchKeyword;
            set { _messageSearchKeyword = value; OnPropertyChanged(); ApplyMessageSearchFilter(); }
        }

        public string UserSearchKeyword
        {
            get => _userSearchKeyword;
            set { _userSearchKeyword = value; OnPropertyChanged(); UpdateFilteredUserList(); }
        }

        public bool IsOpponentTyping =>
            _selectedUser != null &&
            TypingUsers.TryGetValue(_selectedUser, out var isTyping) &&
            isTyping;

        public ICommand SendCommand { get; }
        public ICommand SendFileCommand { get; }
        public ICommand DeleteCommand { get; }

        public MainViewModel() : this("디자이너") { }

        public MainViewModel(string username)
        {
            Nickname = username;
            _chatService = new ChatService(_socket);

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

                    if ((msg.Sender == _selectedUser && msg.Receiver == Nickname) || (msg.Sender == Nickname && msg.Receiver == _selectedUser))
                    {
                        FilteredMessages.Add(msg);
                    }

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

            _socket.PacketReceived += packet =>
                Application.Current.Dispatcher.Invoke(() => _packetHandler.Handle(packet));
            _ = _chatService.ConnectAsync(Nickname);

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

            SendFileCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(_selectedUser))
                {
                    _ = _chatService.SendFileAsync(dlg.FileName, _selectedUser);
                }
            });

            DeleteCommand = new RelayCommand<ChatMessage>(DeleteMessage);
        }

        private void ApplyMessageSearchFilter() => RefreshFilteredMessages();

        private void RefreshFilteredMessages()
        {
            var filtered = MessageFilterHelper.FilterMessagesWithDateHeaders(AllMessages, MessageSearchKeyword, Nickname, _selectedUser);
            FilteredMessages.Clear();
            foreach (var item in filtered)
                FilteredMessages.Add(item);
        }

        private void DeleteMessage(ChatMessage msg)
        {
            if (msg == null || msg.IsDeleted || msg.IsRead) return;
            msg.Message = "삭제된 메시지입니다";
            msg.IsDeleted = true;
            _ = _chatService.SendDeleteAsync(msg);
            RefreshFilteredMessages();
            OnPropertyChanged(nameof(FilteredMessages));
        }

        public async Task RequestFileDownload(string serverPath, string fileName)
            => await _chatService.RequestFileDownloadAsync(serverPath, fileName);

        public async Task RequestFileSendAsync(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(_selectedUser))
            {
                await _chatService.SendFileAsync(filePath, _selectedUser);
            }
        }

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
                    byte[] data = Convert.FromBase64String(packet.Content);
                    File.WriteAllBytes(dlg.FileName, data);
                    MessageBox.Show("파일 저장 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateFilteredUserList()
        {
            FilteredUserList.Clear();
            var source = SelectedFilter == "전체" ? AllUsers : OnlineUsers;
            foreach (var user in source)
            {
                var cleanName = user.Split('(')[0].Trim();
                if (cleanName == Nickname) continue;
                if (!string.IsNullOrWhiteSpace(UserSearchKeyword) &&
                    !cleanName.Contains(UserSearchKeyword, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (UnreadCounts.TryGetValue(cleanName, out int count) && count > 0)
                    FilteredUserList.Add($"{cleanName} ({count})");
                else
                    FilteredUserList.Add(cleanName);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
