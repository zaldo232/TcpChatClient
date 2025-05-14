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

        public string ImageSource => IsImage && !string.IsNullOrWhiteSpace(Content)
            ? $"data:image/png;base64,{Content}"
            : null;

        public bool IsImage =>
            !string.IsNullOrEmpty(FileName) &&
            (FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
             FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
             FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
             FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
             FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override bool Equals(object obj)
        {
            if (obj is not ChatMessage other) return false;
            return Sender == other.Sender &&
                   Receiver == other.Receiver &&
                   Timestamp == other.Timestamp &&
                   FileName == other.FileName &&
                   Content == other.Content;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Sender, Receiver, Timestamp, FileName, Content);
        }
    }
}