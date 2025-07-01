using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using TcpChatClient.Helpers;

// 채팅 메시지 뷰/엔티티 모델 (INotifyPropertyChanged 구현)
public class ChatMessage : INotifyPropertyChanged
{
    public int Id { get; set; }                // 메시지 고유 ID
    public string Sender { get; set; }         // 보낸 사람
    public string Receiver { get; set; }       // 받는 사람
    public string Message { get; set; }        // 평문 메시지(복호화된 내용)
    public string FileName { get; set; }       // 첨부 파일명 (없으면 일반 메시지)
    public string Content { get; set; }        // 파일/이미지(암호화 Base64) 또는 메시지 원문
    public DateTime Timestamp { get; set; }    // 전송 시각
    public string MyName { get; set; }         // 내 닉네임(본인 비교용)

    // 삭제 여부(변경 시 관련 프로퍼티 변경 알림)
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
                OnPropertyChanged(nameof(IsDeletable));
                OnPropertyChanged(nameof(Display));
            }
        }
    }

    // 읽음 여부(변경 시 관련 프로퍼티 변경 알림)
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
                OnPropertyChanged(nameof(IsDeletable));
            }
        }
    }

    public bool IsMine => Sender == MyName;                    // 내 메시지 여부
    public bool IsDeletable => IsMine && !IsDeleted && !IsRead; // 삭제 가능 조건

    public bool IsFile => !string.IsNullOrEmpty(FileName);     // 파일 메시지 여부
    public bool IsFileMessage => !string.IsNullOrEmpty(FileName); // 파일 메시지 여부(동일)

    // 서버저장명에서 원본 파일명 추출
    public string OriginalFileName => FileName?.Split('_').Skip(1).FirstOrDefault() ?? FileName;

    // UI에 표시할 문자열 (삭제/파일/일반 구분)
    public string Display =>
        IsDeleted ? "삭제된 메시지입니다"
        : IsFile ? $"[파일] {OriginalFileName}"
        : Message;

    // 이미지 파일인지 여부
    public bool IsImage => !string.IsNullOrEmpty(FileName) &&
        (FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
         FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
        );

    // 이미지일 경우, 암호화된 내용을 복호화하여 이미지로 반환
    public BitmapImage ImageSource
    {
        get
        {
            if (!IsImage || string.IsNullOrWhiteSpace(Content))
                return null;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(Content);
                byte[] decryptedBytes = AesEncryption.DecryptBytes(encryptedBytes);

                using var ms = new MemoryStream(decryptedBytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                // 이미지 복호화/로딩 실패 시 null
                return null;
            }
        }
    }

    // PropertyChanged 이벤트 구현 (INotifyPropertyChanged)
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // 동등성 비교 오버라이드
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

    // 해시코드 오버라이드
    public override int GetHashCode()
    {
        return HashCode.Combine(Sender, Receiver, Timestamp, FileName, Content);
    }
}
