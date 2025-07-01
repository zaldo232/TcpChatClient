using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using TcpChatClient.Helpers;
using TcpChatClient.ViewModels;

namespace TcpChatClient.Views
{
    // 메인 채팅창(윈도우) 코드비하인드
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;  // 뷰모델 참조

        // 생성자(유저명 지정)
        public MainWindow(string username)
        {
            InitializeComponent();
            _vm = new MainViewModel(username); // 뷰모델 생성
            DataContext = _vm;                // 바인딩

            // 메시지 추가 시 자동 스크롤/읽음처리
            _vm.FilteredMessages.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                { 
                    scrollViewer.ScrollToEnd(); 
                }

                TryMarkAsRead();
            };

            // 스크롤/윈도우 활성화 시 읽음처리
            scrollViewer.ScrollChanged += (s, e) => TryMarkAsRead();
            this.Activated += (_, _) => TryMarkAsRead();
        }

        // 디자이너 미리보기용 생성자
        public MainWindow() : this("디자이너") { }

        // 채팅창이 맨 아래일 때 읽음 처리
        private void TryMarkAsRead()
        {
            if (!IsActive || _vm == null || scrollViewer == null) return;

            bool atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 50;
            if (atBottom) 
            { 
                _ = _vm.MarkMessagesAsReadIfVisible(); 
            }
     
        }

        // 전체 유저 보기 버튼 클릭
        private void ShowAllUsers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            { 
                vm.SelectedFilter = "전체"; 
            }
        }

        // 접속중 유저 보기 버튼 클릭
        private void ShowOnlineUsers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            { 
                vm.SelectedFilter = "접속중"; 
            }
        }

        // 파일, 이미지 클릭(다운로드)
        private void FileDownload_Click(object sender, RoutedEventArgs e)
        {
            ChatMessage msg = null;

            if (sender is FrameworkElement fe && fe.Tag is ChatMessage m)
            {
                msg = m;
            }

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
                    byte[] encrypted = Convert.FromBase64String(msg.Content);
                    byte[] decrypted = AesEncryption.DecryptBytes(encrypted);
                    File.WriteAllBytes(dlg.FileName, decrypted);
                    MessageBox.Show("파일 저장 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 입력창 파일 드래그 허용
        private void InputBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        // 입력창에 파일 드롭 -> 자동 전송
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

        // 플레이스홀더(검색창) 포커스 진입시 텍스트 제거
        private void ClearPlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && (tb.Text == "유저 검색" || tb.Text == "메시지 검색"))
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        // 검색창 포커스 아웃시 플레이스홀더 복구
        private void RestorePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb.Name == "UserSearchBox")
                { 
                    tb.Text = "유저 검색"; 
                }
                else if (tb.Name == "MessageSearchBox")
                {
                    tb.Text = "메시지 검색";
                }

                    tb.Foreground = Brushes.Gray;
            }
        }

        // 유저 검색 텍스트 변경시 뷰모델 반영
        private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm && tb.Text != "유저 검색")
            { 
                vm.UserSearchKeyword = tb.Text; 
            }
        }

        // 메시지 검색 텍스트 변경시 뷰모델 반영
        private void MessageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm && tb.Text != "메시지 검색")
            { 
                vm.MessageSearchKeyword = tb.Text; 
            }
        }

        // 키워드 하이라이팅 표시 (메시지 텍스트 강조)
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

        // 메시지 텍스트 블록 로드시 하이라이트 적용
        private void MessageTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is ChatMessage msg && DataContext is MainViewModel vm)
            {
                tb.Inlines.Clear();
                ApplyHighlightedText(tb, msg.Display, vm.MessageSearchKeyword);
            }
        }

        // 윈도우 활성화시 현재 대화방 읽음 처리 강제
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
