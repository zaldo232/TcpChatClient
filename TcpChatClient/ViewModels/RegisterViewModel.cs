using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Dapper;

namespace TcpChatClient.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        public string Username { get; set; }
        public string Password { private get; set; }
        public string ConfirmPassword { private get; set; }

        public ICommand RegisterCommand { get; }
        public Action CloseWindow { get; set; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(Register);
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("모든 항목을 입력하세요.");
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("비밀번호가 일치하지 않습니다.");
                return;
            }

            const string connStr = "Server=localhost;Database=ChatServerDb;User Id=sa;Password=1234;TrustServerCertificate=True;";
            using var conn = new SqlConnection(connStr);

            var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE Username = @Username", new { Username }) > 0;
            if (exists)
            {
                MessageBox.Show("이미 존재하는 아이디입니다.");
                return;
            }

            var result = conn.Execute("INSERT INTO Users (Username, Password) VALUES (@Username, @Password)", new { Username, Password });
            if (result > 0)
            {
                MessageBox.Show("회원가입 성공!");
                CloseWindow?.Invoke();
            }
            else
            {
                MessageBox.Show("회원가입 실패");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
