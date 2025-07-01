using System.Windows.Input;

namespace TcpChatClient.ViewModels
{
    // 매개변수 없는 MVVM 커맨드 구현 (버튼 등 바인딩용)
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;   // 실행 델리게이트

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged; // (항상 실행 가능 -> 별도 사용X)

        public bool CanExecute(object? parameter) => true; // 항상 실행 가능

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    // 매개변수 있는 MVVM 커맨드 구현
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;    // 실행 델리게이트

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;   // (항상 실행 가능 -> 별도 사용X)

        public bool CanExecute(object? parameter) => true;  // 항상 실행 가능

        public void Execute(object? parameter)
        {
            if (parameter is T value)
            { 
                _execute(value); 
            }
        }
    }

}
