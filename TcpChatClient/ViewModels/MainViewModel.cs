using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text.Json;
using TcpChatClient.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TcpChatClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClientSocket _client = new();
        private string _input;
        private string _selectedUser;
        private string _selectedFilter = "접속중";
        public string Nickname { get; set; }

        public ObservableCollection<ChatMessage> AllMessages { get; } = new();
        public ObservableCollection<ChatMessage> FilteredMessages { get; } = new();
        public ObservableCollection<string> OnlineUsers { get; } = new();
        public ObservableCollection<string> AllUsers { get; } = new();
        public ObservableCollection<string> FilteredUserList { get; } = new();

        private readonly Dictionary<string, int> UnreadCounts = new();

        public string Input
        {
            get => _input;
            set { _input = value; OnPropertyChanged(); }
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

                _ = _client.MarkMessagesAsReadAsync(_selectedUser);

                _ = _client.SendPacketAsync(new ChatPacket
                {
                    Type = "get_history",
                    Sender = Nickname,
                    Receiver = _selectedUser
                });

                FilteredMessages.Clear();
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                UpdateFilteredUserList();

                // ✅ 자동 선택 및 대화 로딩
                if (FilteredUserList.Any())
                    SelectedUser = FilteredUserList.First();
            }
        }

        public ICommand SendCommand { get; }
        public ICommand SendFileCommand { get; }

        public MainViewModel() : this("디자이너") { }

        public MainViewModel(string username)
        {
            Nickname = username;

            SendCommand = new RelayCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(Input) || string.IsNullOrWhiteSpace(_selectedUser))
                {
                    MessageBox.Show("보낼 메시지와 대상 유저를 선택하세요.");
                    return;
                }

                var msg = new ChatMessage
                {
                    Sender = Nickname,
                    Receiver = _selectedUser,
                    Message = Input,
                    MyName = Nickname,
                    Timestamp = DateTime.Now
                };
                AllMessages.Add(msg);
                FilteredMessages.Add(msg);
                await _client.SendMessageAsync(Input, _selectedUser);
                Input = string.Empty;
            });

            SendFileCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(_selectedUser))
                {
                    _ = _client.SendFileAsync(dlg.FileName, _selectedUser);
                }
            });

            _client.PacketReceived += packet =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (packet.Type == "userlist")
                    {
                        OnlineUsers.Clear();
                        foreach (var name in packet.Content.Split(','))
                            if (name != Nickname)
                                OnlineUsers.Add(name);
                        UpdateFilteredUserList();
                        return;
                    }

                    if (packet.Type == "allusers")
                    {
                        AllUsers.Clear();
                        foreach (var name in packet.Content.Split(','))
                            AllUsers.Add(name);
                        UpdateFilteredUserList();

                        // ✅ 자동 선택 초기화
                        if (FilteredUserList.Any() && string.IsNullOrEmpty(_selectedUser))
                            SelectedUser = FilteredUserList.First();
                        return;
                    }

                    if (packet.Type == "history")
                    {
                        var history = JsonSerializer.Deserialize<List<ChatPacket>>(packet.Content);
                        if (history != null)
                        {
                            FilteredMessages.Clear();
                            foreach (var pkt in history)
                            {
                                var chat = new ChatMessage
                                {
                                    Sender = pkt.Sender,
                                    Receiver = pkt.Receiver,
                                    Message = pkt.Type == "file" ? $"[파일] {pkt.FileName}" : pkt.Content,
                                    MyName = Nickname,
                                    Timestamp = pkt.Timestamp
                                };
                                AllMessages.Add(chat);
                                FilteredMessages.Add(chat);
                            }
                        }
                        return;
                    }

                    var msg = new ChatMessage
                    {
                        Sender = packet.Sender,
                        Receiver = packet.Receiver,
                        Message = packet.Type == "file" ? $"[파일 수신] {packet.FileName}" : packet.Content,
                        MyName = Nickname,
                        Timestamp = packet.Timestamp
                    };
                    AllMessages.Add(msg);

                    if ((msg.Sender == _selectedUser && msg.Receiver == Nickname) ||
                        (msg.Sender == Nickname && msg.Receiver == _selectedUser))
                    {
                        FilteredMessages.Add(msg);
                    }
                    else if (msg.Receiver == Nickname)
                    {
                        if (!UnreadCounts.ContainsKey(msg.Sender))
                            UnreadCounts[msg.Sender] = 0;
                        UnreadCounts[msg.Sender]++;
                        UpdateFilteredUserList();
                    }
                });
            };

            _ = _client.ConnectAsync("127.0.0.1", 9000, Nickname);
        }

        private void UpdateFilteredUserList()
        {
            FilteredUserList.Clear();

            var source = SelectedFilter == "전체" ? AllUsers : OnlineUsers;

            foreach (var user in source)
            {
                var cleanName = user.Split('(')[0].Trim();  // 숫자 제거
                if (cleanName == Nickname) continue;

                if (UnreadCounts.TryGetValue(cleanName, out int count) && count > 0)
                    FilteredUserList.Add($"{cleanName} ({count})");
                else
                    FilteredUserList.Add(cleanName);
            }

            // 선택된 유저 강제 재설정 (숫자 없앤 상태로)
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
