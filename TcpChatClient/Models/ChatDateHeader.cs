namespace TcpChatClient.Models
{
    // 채팅 메시지 리스트에서 날짜 구분용 헤더 모델
    public class ChatDateHeader
    {
        public DateTime Date { get; set; }   // 해당 날짜 정보
        public string Display => Date.ToString("yyyy년 M월 d일 (ddd)"); // 표시용 문자열
    }

}
