using Dapper;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace TcpChatClient.ViewModels
{
    // 회원가입 화면용 ViewModel
    public class RegisterViewModel : INotifyPropertyChanged
    {
        public string Username { get; set; }         // 사용자명(아이디)
        public string Password { private get; set; }  // 비밀번호(비공개)
        public string ConfirmPassword { private get; set; } // 비밀번호 확인

        public ICommand RegisterCommand { get; }      // 회원가입 버튼 커맨드
        public Action CloseWindow { get; set; }       // 가입 성공 시 창 닫기 액션

        // 생성자: 커맨드 초기화
        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(Register);
        }

        // 회원가입 처리
        private void Register()
        {
            // 입력값 체크
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

            // 아이디 중복 체크
            var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE Username = @Username", new { Username }) > 0;
            if (exists)
            {
                MessageBox.Show("이미 존재하는 아이디입니다.");
                return;
            }

            // DB에 사용자 정보 저장
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

        // 바인딩용 PropertyChanged 이벤트 구현
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        /*
        public async Task RequestFileDownload(string serverPath, string fileName)
        {
            await _client.SendDownloadRequestAsync(serverPath, fileName);
        }*/
    }
}
