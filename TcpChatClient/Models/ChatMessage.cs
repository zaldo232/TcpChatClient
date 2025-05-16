using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ChatMessage : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Message { get; set; }
    public string FileName { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public string MyName { get; set; }

    private bool _isDeleted;    
    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            if (_isDeleted != value)
            {
                _isDeleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeletable)); // 삭제 버튼 갱신
                OnPropertyChanged(nameof(Display));     // 텍스트 갱신
            }
        }
    }

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
                OnPropertyChanged(nameof(IsDeletable)); // 읽음 상태 변화시 삭제버튼 재계산
            }
        }
    }

    public bool IsMine => Sender == MyName;
    public bool IsDeletable => IsMine && !IsDeleted && !IsRead;

    public bool IsFile => !string.IsNullOrEmpty(FileName);
    public bool IsFileMessage => !string.IsNullOrEmpty(FileName);
    public string OriginalFileName => FileName?.Split('_').Skip(1).FirstOrDefault() ?? FileName;

    public string Display =>
        IsDeleted ? "삭제된 메시지입니다"
        : IsFile ? $"[파일] {OriginalFileName}"
        : Message;

    public string ImageSource => IsImage && !string.IsNullOrWhiteSpace(Content) ? $"data:image/png;base64,{Content}" : null;

    public bool IsImage =>
        !string.IsNullOrEmpty(FileName) &&
        (FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override bool Equals(object obj)
    {
        if (obj is not ChatMessage other) return false;
        return Sender == other.Sender &&
               Receiver == other.Receiver &&
               Timestamp == other.Timestamp &&
               FileName == other.FileName &&
               Content == other.Content &&
               IsDeleted == other.IsDeleted;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Sender, Receiver, Timestamp, FileName, Content);
    }
}
