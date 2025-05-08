using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using TcpChatClient.Models;

namespace TcpChatClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ClientSocket _client = new ClientSocket();
        private string _input;
        public string Nickname { get; set; }

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public string Input
        {
            get => _input;
            set
            {
                _input = value;
                OnPropertyChanged();
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
                if (!string.IsNullOrWhiteSpace(Input))
                {
                    await _client.SendMessageAsync(Input);
                    Messages.Add(new ChatMessage
                    {
                        Sender = Nickname,
                        Message = Input
                    });
                    Input = string.Empty;
                }
            });

            SendFileCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true)
                {
                    _ = _client.SendFileAsync(dlg.FileName);
                }
            });

            _client.PacketReceived += packet =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (packet.Type == "file")
                    {
                        try
                        {
                            byte[] bytes = Convert.FromBase64String(packet.Content);

                            var dlg = new SaveFileDialog
                            {
                                Title = "파일 저장 위치 선택",
                                FileName = packet.FileName,
                                Filter = "모든 파일|*.*"
                            };

                            if (dlg.ShowDialog() == true)
                            {
                                File.WriteAllBytes(dlg.FileName, bytes);

                                Messages.Add(new ChatMessage
                                {
                                    Sender = packet.Sender,
                                    Message = $"[파일 저장됨] {Path.GetFileName(dlg.FileName)}"
                                });
                            }
                            else
                            {
                                Messages.Add(new ChatMessage
                                {
                                    Sender = packet.Sender,
                                    Message = $"[파일 수신 취소] {packet.FileName}"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Messages.Add(new ChatMessage
                            {
                                Sender = "시스템",
                                Message = $"[파일 저장 실패] {ex.Message}"
                            });
                        }
                    }
                    else
                    {
                        Messages.Add(new ChatMessage
                        {
                            Sender = packet.Sender,
                            Message = packet.Content
                        });
                    }
                });
            };

            _ = _client.ConnectAsync("127.0.0.1", 9000, Nickname);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
