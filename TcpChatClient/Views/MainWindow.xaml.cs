using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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

        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && (tb.Text == "유저 검색" || tb.Text == "메시지 검색"))
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb.Name == "UserSearchBox")
                    tb.Text = "유저 검색";
                else if (tb.Name == "MessageSearchBox")
                    tb.Text = "메시지 검색";

                tb.Foreground = Brushes.Gray;
            }
        }

        private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm && tb.Text != "유저 검색")
                vm.UserSearchKeyword = tb.Text;
        }

        private void MessageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm && tb.Text != "메시지 검색")
                vm.MessageSearchKeyword = tb.Text;
        }

        public static void ApplyHighlightedText(TextBlock target, string fullText, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                target.Inlines.Add(fullText);
                return;
            }

            int index = 0;
            int matchIndex;
            while ((matchIndex = fullText.IndexOf(keyword, index, System.StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (matchIndex > index)
                {
                    target.Inlines.Add(new Run(fullText.Substring(index, matchIndex - index)));
                }

                var highlight = new Run(fullText.Substring(matchIndex, keyword.Length))
                {
                    Background = Brushes.Yellow,
                    FontWeight = FontWeights.Bold
                };
                target.Inlines.Add(highlight);

                index = matchIndex + keyword.Length;
            }

            if (index < fullText.Length)
            {
                target.Inlines.Add(new Run(fullText.Substring(index)));
            }
        }

        private void MessageTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is ChatMessage msg && DataContext is MainViewModel vm)
            {
                tb.Inlines.Clear();
                ApplyHighlightedText(tb, msg.Display, vm.MessageSearchKeyword);
            }
        }

    }
}
