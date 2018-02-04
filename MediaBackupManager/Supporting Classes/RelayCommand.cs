using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MediaBackupManager.SupportingClasses
{
    public class RelayCommand : ICommand, IDisposable
    {
        readonly Predicate<object> _canExecute;
        readonly Action<object> _execute;
        List<EventHandler> _canExecuteSubscribers = new List<EventHandler>();
        bool disposed;

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand()
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _canExecuteSubscribers.Add(value);
                //Logger("Add " + value.Target.GetType().Name);
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                //_canExecuteSubscribers.Remove(value);
                //Logger("Remove " + value.Target.GetType().Name);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || disposed ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void Dispose()
        {
            //Logger("RelayCommand Dispose");
            _canExecuteSubscribers.ForEach(h => CanExecuteChanged -= h);
            _canExecuteSubscribers.Clear();
            disposed = true;
        }

        //private void Logger(string text)
        //{
        //    StreamWriter sw = new StreamWriter("D:\\Temp\\RelayCommand.txt", true);
        //    sw.WriteLine(String.Format("{0:G} ", DateTime.Now) + text);
        //    sw.Dispose();
        //}

    }
}
