using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TcpChatClient.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string MyName { get; set; }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMine => Sender == MyName;
        public bool IsFile => !string.IsNullOrEmpty(FileName);
        public bool IsFileMessage => !string.IsNullOrEmpty(FileName);
        public string OriginalFileName => FileName?.Split('_').Skip(1).FirstOrDefault() ?? FileName;

        public string Display => IsFile
            ? $"[파일] {OriginalFileName}"
            : $"{Message}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
