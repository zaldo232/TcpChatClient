using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using TcpChatClient.Models;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;

        public MainWindow(string username)
        {
            InitializeComponent();
            _vm = new MainViewModel(username);
            DataContext = _vm;

            _vm.FilteredMessages.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    scrollViewer.ScrollToEnd();
            };
        }

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

        private async void FileDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatMessage msg)
            {
                if (msg.MyName != msg.Sender && !string.IsNullOrWhiteSpace(msg.Content))
                {
                    await _vm.RequestFileDownload(msg.Content, msg.FileName);
                }
            }
        }

        private void InputBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void InputBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (DataContext is MainViewModel vm && !string.IsNullOrEmpty(vm.SelectedUser))
                    {
                        _ = vm.RequestFileSendAsync(file);
                    }
                }
            }
        }

    }
}
