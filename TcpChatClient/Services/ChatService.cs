using TcpChatClient.Models;

namespace TcpChatClient.Services
{
    // 채팅 기능(비즈니스 로직) 서비스 클래스
    public class ChatService
    {
        private readonly ClientSocket _socket;   // 실제 네트워크 연결 및 패킷 송수신 담당

        // 생성자: 클라이언트 소켓 DI
        public ChatService(ClientSocket socket)
        {
            _socket = socket;
        }

        // 서버 연결 시도 (닉네임 입력)
        public Task ConnectAsync(string nickname) => _socket.ConnectAsync("127.0.0.1", 9000, nickname);

        // 메시지 전송
        public Task SendMessageAsync(string text, string receiver) => _socket.SendMessageAsync(text, receiver);

        // 파일 전송
        public Task SendFileAsync(string filePath, string receiver) => _socket.SendFileAsync(filePath, receiver);

        // 상대방과의 메시지 전체 읽음 처리 요청
        public Task MarkMessagesAsReadAsync(string withUser) => _socket.MarkMessagesAsReadAsync(withUser);

        // 서버 파일 다운로드 요청
        public Task RequestFileDownloadAsync(string serverPath, string fileName) => _socket.SendDownloadRequestAsync(serverPath, fileName);

        // 채팅 이력(히스토리) 요청
        public Task RequestHistoryAsync(string from, string to) => _socket.SendPacketAsync(new ChatPacket
        {
            Type = "get_history",
            Sender = from,
            Receiver = to
        });

        // 메시지 삭제 요청
        public Task SendDeleteAsync(ChatMessage msg)
        {
            var packet = new ChatPacket
            {
                Type = "delete",
                Sender = msg.Sender,
                Receiver = msg.Receiver,
                Timestamp = msg.Timestamp,
                Id = msg.Id
            };
            return _socket.SendPacketAsync(packet);
        }

    }
}
