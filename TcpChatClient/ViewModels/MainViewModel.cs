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

                    var msg = new ChatMessage
                    {
                        Sender = Nickname,
                        Receiver = _selectedUser,
                        Message = $"[파일] {Path.GetFileName(dlg.FileName)}",
                        MyName = Nickname,
                        Timestamp = DateTime.Now,
                        FileName = Path.GetFileName(dlg.FileName),
                        Content = "" // 서버 경로 나중에 채워짐
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
                            foreach (var pkt in history)
                            {
                                var chat = new ChatMessage
                                {
                                    Sender = pkt.Sender,
                                    Receiver = pkt.Receiver,
                                    Message = pkt.Type == "file" ? $"[파일] {pkt.FileName}" : pkt.Content,
                                    MyName = Nickname,
                                    Timestamp = pkt.Timestamp,
                                    FileName = pkt.FileName,
                                    Content = pkt.Content
                                };

                                if (!AllMessages.Any(m =>
                                    m.Sender == chat.Sender &&
                                    m.Receiver == chat.Receiver &&
                                    m.Timestamp == chat.Timestamp &&
                                    m.FileName == chat.FileName))
                                {
                                    AllMessages.Add(chat);
                                }

                                if ((chat.Sender == _selectedUser && chat.Receiver == Nickname) ||
                                    (chat.Sender == Nickname && chat.Receiver == _selectedUser))
                                {
                                    FilteredMessages.Add(chat);
                                }
                            }
                        }
                        return;
                    }

                    if (packet.Type == "download_result")
                    {
                        SaveDownloadToFile(packet);
                        return;
                    }

                    var msg = new ChatMessage
                    {
                        Sender = packet.Sender,
                        Receiver = packet.Receiver,
                        Message = packet.Type == "file" ? $"[파일 수신] {packet.FileName}" : packet.Content,
                        MyName = Nickname,
                        Timestamp = packet.Timestamp,
                        FileName = packet.FileName,
                        Content = packet.Content
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

                if (UnreadCounts.TryGetValue(cleanName, out int count) && count > 0)
                    FilteredUserList.Add($"{cleanName} ({count})");
                else
                    FilteredUserList.Add(cleanName);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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
                    Content = "" // 서버가 실제 경로로 채워줌
                };
                AllMessages.Add(msg);
                FilteredMessages.Add(msg);
            }
        }

    }
}
