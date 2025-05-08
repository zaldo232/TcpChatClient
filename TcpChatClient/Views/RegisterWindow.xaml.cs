using System.Windows;
using System.Windows.Controls;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel();
            _viewModel.CloseWindow = Close;
            DataContext = _viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = ((PasswordBox)sender).Password;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}
