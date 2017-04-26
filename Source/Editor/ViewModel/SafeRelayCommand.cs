using System;
using System.Windows;
using System.Windows.Input;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class SafeRelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public SafeRelayCommand(Action execute)
        {
            this.execute = execute;
        }

        public SafeRelayCommand(Action execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        #region Implementation of ICommand

        public void Execute(object parameter)
        {
            SafeOperation(() => execute.Invoke());
        }

        public bool CanExecute(object parameter)
        {
            if (execute == null)
                return false;

            return canExecute == null || canExecute.Invoke();
        }

        public event EventHandler CanExecuteChanged;

        public void OnCanExecuteChanged(EventArgs e)
        {
            var handler = CanExecuteChanged;
            if (handler != null) handler(this, e);
        }

        #endregion

        private static void SafeOperation(Action annonymousMethod)
        {
            try
            {
                annonymousMethod.Invoke();
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show(e.ToString(), "Unexpected Error");
#else
                MessageBox.Show(e.Message, "Unexpected Error");
#endif
            }
        }
    }
}
