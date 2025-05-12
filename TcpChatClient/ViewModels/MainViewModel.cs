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
        private string _messageSearchKeyword;
        private string _userSearchKeyword;

        public string Nickname { get; set; }

        public ObservableCollection<ChatMessage> AllMessages { get; } = new();
        public ObservableCollection<object> FilteredMessages { get; } = new();
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

                ApplyMessageSearchFilter();
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
            }
        }

        public string MessageSearchKeyword
        {
            get => _messageSearchKeyword;
            set
            {
                _messageSearchKeyword = value;
                OnPropertyChanged();
                ApplyMessageSearchFilter();
            }
        }

        public string UserSearchKeyword
        {
            get => _userSearchKeyword;
            set
            {
                _userSearchKeyword = value;
                OnPropertyChanged();
                UpdateFilteredUserList();
            }
        }

        public ICommand SendCommand { get; }
        public ICommand SendFileCommand { get; }
        public ClientSocket Client => _client;

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
                    Timestamp = DateTime.Now,
                    IsRead = false
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

                    var msg = new ChatMessage
                    {
                        Sender = Nickname,
                        Receiver = _selectedUser,
                        Message = $"[파일] {Path.GetFileName(dlg.FileName)}",
                        MyName = Nickname,
                        Timestamp = DateTime.Now,
                        FileName = Path.GetFileName(dlg.FileName),
                        Content = "",
                        IsRead = false
                    };
                    AllMessages.Add(msg);
                    FilteredMessages.Add(msg);
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
                        UnreadCounts.Clear();

                        foreach (var raw in packet.Content.Split(','))
                        {
                            var parts = raw.Split('(');
                            var name = parts[0].Trim();

                            AllUsers.Add(name);

                            if (parts.Length > 1 && int.TryParse(parts[1].TrimEnd(')', ' '), out int count))
                                UnreadCounts[name] = count;
                        }

                        UpdateFilteredUserList();
                        return;
                    }

                    if (packet.Type == "history")
                    {
                        var history = JsonSerializer.Deserialize<List<ChatPacket>>(packet.Content);
                        if (history != null)
                        {
                            FilteredMessages.Clear();
                            DateTime? lastDate = null;

                            foreach (var pkt in history.OrderBy(p => p.Timestamp))
                            {
                                var chat = new ChatMessage
                                {
                                    Sender = pkt.Sender,
                                    Receiver = pkt.Receiver,
                                    Message = pkt.Type == "file" ? $"[파일] {pkt.FileName}" : pkt.Content,
                                    MyName = Nickname,
                                    Timestamp = pkt.Timestamp,
                                    FileName = pkt.FileName,
                                    Content = pkt.Content,
                                    IsRead = pkt.IsRead
                                };

                                if (!AllMessages.Any(m =>
                                    m.Sender == chat.Sender &&
                                    m.Receiver == chat.Receiver &&
                                    m.Timestamp == chat.Timestamp &&
                                    m.FileName == chat.FileName))
                                {
                                    AllMessages.Add(chat);
                                }

                                bool isRelevant = (chat.Sender == _selectedUser && chat.Receiver == Nickname) ||
                                                  (chat.Sender == Nickname && chat.Receiver == _selectedUser);

                                if (isRelevant)
                                {
                                    var dateOnly = chat.Timestamp.Date;
                                    if (lastDate == null || lastDate.Value != dateOnly)
                                    {
                                        FilteredMessages.Add(new ChatDateHeader { Date = dateOnly });
                                        lastDate = dateOnly;
                                    }

                                    FilteredMessages.Add(chat);
                                }
                            }
                        }
                        return;
                    }

                    if (packet.Type == "read_notify")
                    {
                        foreach (var msg in AllMessages.Where(m =>
                            m.Receiver == packet.Receiver &&
                            m.Sender == packet.Sender))
                        {
                            msg.IsRead = true;
                        }
                        return;
                    }

                    if (packet.Type == "download_result")
                    {
                        SaveDownloadToFile(packet);
                        return;
                    }

                    var newMsg = new ChatMessage
                    {
                        Sender = packet.Sender,
                        Receiver = packet.Receiver,
                        Message = packet.Type == "file" ? $"[파일 수신] {packet.FileName}" : packet.Content,
                        MyName = Nickname,
                        Timestamp = packet.Timestamp,
                        FileName = packet.FileName,
                        Content = packet.Content,
                        IsRead = packet.IsRead
                    };
                    AllMessages.Add(newMsg);

                    if ((newMsg.Sender == _selectedUser && newMsg.Receiver == Nickname) ||
                        (newMsg.Sender == Nickname && newMsg.Receiver == _selectedUser))
                    {
                        FilteredMessages.Add(newMsg);
                    }
                    else if (newMsg.Receiver == Nickname)
                    {
                        if (!UnreadCounts.ContainsKey(newMsg.Sender))
                            UnreadCounts[newMsg.Sender] = 0;
                        UnreadCounts[newMsg.Sender]++;
                        UpdateFilteredUserList();
                    }
                });
            };

            _ = _client.ConnectAsync("127.0.0.1", 9000, Nickname);
        }

        private void ApplyMessageSearchFilter()
        {
            FilteredMessages.Clear();
            DateTime? lastDate = null;

            var filtered = AllMessages
                .Where(m =>
                    (m.Sender == _selectedUser && m.Receiver == Nickname) ||
                    (m.Sender == Nickname && m.Receiver == _selectedUser))
                .Where(m =>
                    string.IsNullOrWhiteSpace(MessageSearchKeyword) ||
                    m.Message.Contains(MessageSearchKeyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Timestamp);

            foreach (var msg in filtered)
            {
                var dateOnly = msg.Timestamp.Date;
                if (lastDate == null || lastDate.Value != dateOnly)
                {
                    FilteredMessages.Add(new ChatDateHeader { Date = dateOnly });
                    lastDate = dateOnly;
                }
                FilteredMessages.Add(msg);
            }
        }

        public async Task RequestFileDownload(string serverPath, string fileName)
        {
            await _client.SendDownloadRequestAsync(serverPath, fileName);
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

        public async Task RequestFileSendAsync(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(_selectedUser))
            {
                await _client.SendFileAsync(filePath, _selectedUser);

                var msg = new ChatMessage
                {
                    Sender = Nickname,
                    Receiver = _selectedUser,
                    Message = $"[파일] {Path.GetFileName(filePath)}",
                    MyName = Nickname,
                    Timestamp = DateTime.Now,
                    FileName = Path.GetFileName(filePath),
                    Content = "",
                    IsRead = false
                };
                AllMessages.Add(msg);
                FilteredMessages.Add(msg);
            }
        }

        public async Task MarkMessagesAsReadIfVisible()
        {
            if (!string.IsNullOrWhiteSpace(_selectedUser))
            {
                await _client.MarkMessagesAsReadAsync(_selectedUser);
                if (UnreadCounts.ContainsKey(_selectedUser))
                {
                    UnreadCounts[_selectedUser] = 0;
                    UpdateFilteredUserList();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
