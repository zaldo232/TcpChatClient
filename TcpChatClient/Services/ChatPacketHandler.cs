using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TcpChatClient.Models;

namespace TcpChatClient.Services
{
    public class ChatPacketHandler
    {
        private readonly string _myName;
        private readonly Action<List<string>> _updateOnlineUsers;
        private readonly Action<List<(string user, int count)>> _updateAllUsers;
        private readonly Action<List<ChatPacket>> _loadHistory;
        private readonly Action<ChatPacket> _handleNewMessage;
        private readonly Action<ChatPacket> _handleDownload;
        private readonly Action<string, string> _markReadNotify;
        private readonly Action<string, string, DateTime, int> _handleDeleteNotify;

        public ChatPacketHandler(
            string myName,
            Action<List<string>> updateOnlineUsers,
            Action<List<(string user, int count)>> updateAllUsers,
            Action<List<ChatPacket>> loadHistory,
            Action<ChatPacket> handleNewMessage,
            Action<ChatPacket> handleDownload,
            Action<string, string> markReadNotify,
            Action<string, string, DateTime, int> handleDeleteNotify)
        {
            _myName = myName;
            _updateOnlineUsers = updateOnlineUsers;
            _updateAllUsers = updateAllUsers;
            _loadHistory = loadHistory;
            _handleNewMessage = handleNewMessage;
            _handleDownload = handleDownload;
            _markReadNotify = markReadNotify;
            _handleDeleteNotify = handleDeleteNotify;
        }

        public void Handle(ChatPacket packet)
        {
            switch (packet.Type)
            {
                case "userlist":
                    var online = packet.Content.Split(',').Where(x => x != _myName).ToList();
                    _updateOnlineUsers(online);
                    break;

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

                case "history":
                    var history = JsonSerializer.Deserialize<List<ChatPacket>>(packet.Content);
                    if (history != null)
                        _loadHistory(history);
                    break;

                case "download_result":
                    _handleDownload(packet);
                    break;

                case "read_notify":
                    _markReadNotify(packet.Sender, packet.Receiver);
                    break;

                case "delete_notify":
                    _handleDeleteNotify(packet.Sender, packet.Receiver, packet.Timestamp, packet.Id);
                    break;

                default:
                    _handleNewMessage(packet);
                    break;
            }
        }
    }

}
