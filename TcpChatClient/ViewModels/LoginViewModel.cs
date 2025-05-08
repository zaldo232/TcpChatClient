using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using TcpChatClient.Views;
using Dapper;

namespace TcpChatClient.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public string Username { get; set; }
        public string Password { private get; set; } // PasswordBox 처리 따로 함

        public ICommand LoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            OpenRegisterCommand = new RelayCommand(OpenRegister);
        }

        private void ExecuteLogin()
        {
            const string connStr = "Server=localhost;Database=ChatServerDb;User Id=sa;Password=1234;TrustServerCertificate=True;";
            using var conn = new SqlConnection(connStr);
            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";

            var success = conn.ExecuteScalar<int>(sql, new { Username, Password }) > 0;
            if (success)
            {
                var chat = new MainWindow(Username);
                chat.Show();
                CloseWindow?.Invoke();
            }
            else
            {
                MessageBox.Show("로그인 실패: 아이디 또는 비밀번호가 틀렸습니다.");
            }
        }

        private void OpenRegister()
        {
            new RegisterWindow().ShowDialog();
        }

        public Action CloseWindow { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
