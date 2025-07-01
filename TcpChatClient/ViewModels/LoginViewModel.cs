using Dapper;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TcpChatClient.Views;

namespace TcpChatClient.ViewModels
{
    // 로그인 화면용 ViewModel
    public class LoginViewModel : INotifyPropertyChanged
    {
        public string Username { get; set; }   // 입력된 사용자명
        public string Password { private get; set; } // 입력된 비밀번호(비공개)

        public ICommand LoginCommand { get; }           // 로그인 버튼 커맨드
        public ICommand OpenRegisterCommand { get; }    // 회원가입창 오픈 커맨드

        // 생성자: 커맨드 바인딩
        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            OpenRegisterCommand = new RelayCommand(OpenRegister);
        }

        // 로그인 버튼 클릭 시 실행
        private void ExecuteLogin()
        {
            const string connStr = "Server=localhost;Database=ChatServerDb;User Id=sa;Password=1234;TrustServerCertificate=True;";
            using var conn = new SqlConnection(connStr);
            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";

            // 사용자 인증(DB 조회)
            var success = conn.ExecuteScalar<int>(sql, new { Username, Password }) > 0;
            if (success)
            {
                // 성공: 메인 윈도우 오픈
                var chat = new MainWindow(Username);
                chat.Show();
                CloseWindow?.Invoke();
            }
            else
            {
                // 실패: 메시지 출력
                MessageBox.Show("로그인 실패: 아이디 또는 비밀번호가 틀렸습니다.");
            }
        }

        // 회원가입 창 오픈
        private void OpenRegister()
        {
            new RegisterWindow().ShowDialog();
        }

        // 로그인 창 닫기 액션
        public Action CloseWindow { get; set; }

        // 바인딩용 PropertyChanged 이벤트 구현
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
