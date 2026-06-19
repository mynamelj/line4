using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MES
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// The execute
        /// </summary>
        private readonly Action _execute;
        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Func<bool> _canExecute;
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        public RelayCommand(Action execute) : this(execute, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="System.ArgumentNullException">execute</exception>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        /// <summary>
        /// 定义确定此命令是否可在其当前状态下执行的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        /// <returns>
        /// 如果可执行此命令，则为 <see langword="true" />；否则为 <see langword="false" />。
        /// </returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute();
        }
        /// <summary>
        /// 定义在调用此命令时要调用的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        void ICommand.Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// Determines whether this instance can execute.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute()
        {
            if (_canExecute == null)
            {
                return true;
            } 
            return _canExecute();
        }
        /// <summary>
        /// Executes this instance.
        /// </summary>
        public void Execute()
        {
            _execute();
        }

        /// <summary>
        /// Called when [can execute changed].
        /// </summary>
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 当出现影响是否应执行该命令的更改时发生。
        /// </summary>
        public event EventHandler CanExecuteChanged;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommand<T> : ICommand
    {
        /// <summary>
        /// The execute
        /// </summary>
        private readonly Action<T> _execute;
        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Func<T, bool> _canExecute;
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        public RelayCommand(Action<T> execute) : this(execute, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="System.ArgumentNullException">execute</exception>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        /// <summary>
        /// 定义确定此命令是否可在其当前状态下执行的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        /// <returns>
        /// 如果可执行此命令，则为 <see langword="true" />；否则为 <see langword="false" />。
        /// </returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute((T)parameter);
        }
        /// <summary>
        /// 定义在调用此命令时要调用的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        void ICommand.Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// Determines whether this instance can execute the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        ///   <c>true</c> if this instance can execute the specified parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(T parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute(parameter);
        }
        /// <summary>
        /// Executes the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        public void Execute(T parameter)
        {
            _execute(parameter);
        }


        /// <summary>
        /// 当出现影响是否应执行该命令的更改时发生。
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Called when [can execute changed].
        /// </summary>
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }




    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommandAsync : ICommand
    {
        /// <summary>
        /// The execute
        /// </summary>
        private readonly Func<Task> _execute;
        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Func<bool> _canExecute;
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAsync"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        public RelayCommandAsync(Func<Task> execute) : this(execute, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAsync"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="System.ArgumentNullException">execute</exception>
        public RelayCommandAsync(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        /// <summary>
        /// 定义确定此命令是否可在其当前状态下执行的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        /// <returns>
        /// 如果可执行此命令，则为 <see langword="true" />；否则为 <see langword="false" />。
        /// </returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute();
        }

        /// <summary>
        /// 定义在调用此命令时要调用的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        async void ICommand.Execute(object parameter)
        {
            await _execute();
        }

        /// <summary>
        /// Determines whether this instance can execute.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can execute; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute()
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute();
        }
        /// <summary>
        /// Executes the asynchronous.
        /// </summary>
        public async Task ExecuteAsync()
        {
            await _execute();
        }

        /// <summary>
        /// Called when [can execute changed].
        /// </summary>
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 当出现影响是否应执行该命令的更改时发生。
        /// </summary>
        public event EventHandler CanExecuteChanged;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommandAsync<T> : ICommand
    {
        /// <summary>
        /// The execute
        /// </summary>
        private readonly Func<T, Task> _execute;
        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Func<T, bool> _canExecute;
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAsync{T}"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        public RelayCommandAsync(Func<T, Task> execute) : this(execute, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommandAsync{T}"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="System.ArgumentNullException">execute</exception>
        public RelayCommandAsync(Func<T, Task> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }
        /// <summary>
        /// 定义确定此命令是否可在其当前状态下执行的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        /// <returns>
        /// 如果可执行此命令，则为 <see langword="true" />；否则为 <see langword="false" />。
        /// </returns>
        bool ICommand.CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute((T)parameter);
        }
        /// <summary>
        /// 定义在调用此命令时要调用的方法。
        /// </summary>
        /// <param name="parameter">此命令使用的数据。  如果此命令不需要传递数据，则该对象可以设置为 <see langword="null" />。</param>
        async void ICommand.Execute(object parameter)
        {
            await _execute((T)parameter);
        }

        /// <summary>
        /// Determines whether this instance can execute the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        ///   <c>true</c> if this instance can execute the specified parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool CanExecute(T parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute(parameter);
        }
        /// <summary>
        /// Executes the asynchronous.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        public async Task ExecuteAsync(T parameter)
        {
            await _execute(parameter);
        }


        /// <summary>
        /// 当出现影响是否应执行该命令的更改时发生。
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Called when [can execute changed].
        /// </summary>
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
