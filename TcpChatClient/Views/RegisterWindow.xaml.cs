using System.Windows;
using System.Windows.Controls;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    // 회원가입 창(윈도우) 코드비하인드
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel; // 뷰모델 인스턴스

        // 회원가입 창 생성자
        public RegisterWindow()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel(); // 뷰모델 생성
            _viewModel.CloseWindow = Close;       // 닫기 액션 연결
            DataContext = _viewModel;             // 데이터컨텍스트 바인딩
        }

        // 비밀번호 입력 변경 시 ViewModel에 전달
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = ((PasswordBox)sender).Password;
        }

        // 비밀번호 확인 입력 변경 시 ViewModel에 전달
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}
