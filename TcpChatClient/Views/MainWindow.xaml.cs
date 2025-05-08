using System.Windows;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(string username)
        {
            InitializeComponent();
            DataContext = new MainViewModel(username);
        }

        // 디자이너용 기본 생성자
        public MainWindow() : this("디자이너") { }
    }
}
