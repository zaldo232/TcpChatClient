using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using TcpChatClient.Models;
using TcpChatClient.ViewModels;
using System.IO;

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

                TryMarkAsRead();
            };

            scrollViewer.ScrollChanged += (s, e) => TryMarkAsRead();
            this.Activated += (_, _) => TryMarkAsRead();
        }

        public MainWindow() : this("디자이너") { }

        private void TryMarkAsRead()
        {
            if (!IsActive || _vm == null || scrollViewer == null) return;

            bool atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 50;
            if (atBottom)
                _ = _vm.MarkMessagesAsReadIfVisible();
        }

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
            ChatMessage msg = null;

            if (sender is FrameworkElement fe && fe.Tag is ChatMessage m)
                msg = m;

            if (msg == null || string.IsNullOrWhiteSpace(msg.Content)) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = msg.OriginalFileName,
                Title = "파일 저장",
                Filter = "모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] data = Convert.FromBase64String(msg.Content);
                    File.WriteAllBytes(dlg.FileName, data);
                    MessageBox.Show("파일 저장 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrEmpty(fullText))
            {
                target.Inlines.Add(fullText ?? "(빈 메세지)");
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // 뷰모델 가져오기
            if (DataContext is MainViewModel vm)
            {
                _ = vm.MarkMessagesAsReadIfVisible(); // 현재 열린 상대에 대해 읽음 처리 강제 호출
            }
        }

    }
}
