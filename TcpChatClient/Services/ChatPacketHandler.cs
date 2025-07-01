using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TcpChatClient.Helpers;
using TcpChatClient.Models;

namespace TcpChatClient.Services
{
    // 채팅 패킷 타입별 처리 담당 클래스 (ViewModel에서 사용)
    public class ChatPacketHandler
    {
        private readonly string _myName;       // 내 닉네임
        private readonly Action<List<string>> _updateOnlineUsers;     // 접속중 유저리스트 업데이트 액션
        private readonly Action<List<(string user, int count)>> _updateAllUsers; // 전체 유저(미읽음 수 포함) 업데이트
        private readonly Action<List<ChatPacket>> _loadHistory;       // 대화 이력 불러오기
        private readonly Action<ChatPacket> _handleNewMessage;        // 새 메시지 처리
        private readonly Action<ChatPacket> _handleDownload;          // 파일 다운로드 처리
        private readonly Action<string, string> _markReadNotify;      // 읽음 알림 처리
        private readonly Action<string, string, DateTime, int> _handleDeleteNotify; // 메시지 삭제 알림 처리
        private readonly Action<string, bool> _setTypingState;        // 타이핑 상태 표시

        // 생성자 (핸들러마다 실제 동작을 외부에서 DI)
        public ChatPacketHandler(
            string myName,
            Action<List<string>> updateOnlineUsers,
            Action<List<(string user, int count)>> updateAllUsers,
            Action<List<ChatPacket>> loadHistory,
            Action<ChatPacket> handleNewMessage,
            Action<ChatPacket> handleDownload,
            Action<string, string> markReadNotify,
            Action<string, string, DateTime, int> handleDeleteNotify,
            Action<string, bool> setTypingState)
        {
            _myName = myName;
            _updateOnlineUsers = updateOnlineUsers;
            _updateAllUsers = updateAllUsers;
            _loadHistory = loadHistory;
            _handleNewMessage = handleNewMessage;
            _handleDownload = handleDownload;
            _markReadNotify = markReadNotify;
            _handleDeleteNotify = handleDeleteNotify;
            _setTypingState = setTypingState;
        }

        // 서버에서 받은 패킷 타입별 분기 처리
        public void Handle(ChatPacket packet)
        {
            switch (packet.Type)
            {
                // 현재 접속 유저리스트 수신 시
                case "userlist":
                    var online = packet.Content.Split(',').Where(x => x != _myName).ToList();
                    _updateOnlineUsers(online);
                    break;

                // 전체 유저리스트(미읽음 포함) 수신 시
                case "allusers":
                    var list = packet.Content.Split(',')
                        .Select(name =>
                        {
                            var parts = name.Split('(');
                            var clean = parts[0].Trim();
                            int count = parts.Length > 1 && int.TryParse(parts[1].TrimEnd(')', ' '), out int c) ? c : 0;
                            return (clean, count);
                        }).ToList();
                    _updateAllUsers(list);
                    break;

                // 채팅 히스토리(이력) 수신 시
                case "history":
                    var history = JsonSerializer.Deserialize<List<ChatPacket>>(packet.Content);
                    if (history != null)
                    {
                        _loadHistory(history);
                    }
                    break;

                // 파일 다운로드 응답 처리
                case "download_result":
                    _handleDownload(packet);
                    break;

                // 읽음 처리 알림
                case "read_notify":
                    _markReadNotify(packet.Sender, packet.Receiver);
                    break;

                // 메시지 삭제 알림
                case "delete_notify":
                    _handleDeleteNotify(packet.Sender, packet.Receiver, packet.Timestamp, packet.Id);
                    break;

                // 타이핑 상태 처리
                case "typing":
                    if (packet.Content == "start")
                        _setTypingState(packet.Sender, true);
                    else
                        _setTypingState(packet.Sender, false);
                    break;

                // 기본: 새 메시지/파일 도착
                default:
                    if (packet.Type == "message" || packet.Type == "file")
                        _handleNewMessage(packet);
                    break;
            }
        }

    }
}
