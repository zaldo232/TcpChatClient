using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TcpChatClient.Models;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(string username)
        {
            InitializeComponent();

            var vm = new MainViewModel(username);
            DataContext = vm;

            vm.FilteredMessages.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    scrollViewer.ScrollToEnd();
            };
        }

        // 디자이너용 기본 생성자
        public MainWindow() : this("디자이너") { }

        private void ShowAllUsers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SelectedFilter = "전체";
        }

        private void ShowOnlineUsers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SelectedFilter = "접속중";
        }

        private void FileDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatMessage msg)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = msg.FileName,
                    Title = "파일 저장",
                    Filter = "모든 파일 (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(msg.Content); // 서버에서 받은 경로를 사용
                        File.WriteAllBytes(dialog.FileName, data);
                        MessageBox.Show("파일 저장 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show("파일 읽기 실패: " + ex.Message, "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
