using System.Windows;
using System.Windows.Controls;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    // 로그인 창(윈도우)
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel; // ViewModel 인스턴스

        // 로그인 창 생성자
        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel(); // ViewModel 생성
            _viewModel.CloseWindow = Close;    // 닫기 액션 연결
            DataContext = _viewModel;          // 데이터컨텍스트 바인딩
        }

        // 비밀번호 입력 변경시 ViewModel에 전달
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = ((PasswordBox)sender).Password;
        }
    }
}
