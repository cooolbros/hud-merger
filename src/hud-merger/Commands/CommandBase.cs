using System;
using System.Windows.Input;

namespace HUDMerger.Commands
{
	public abstract class CommandBase : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public virtual bool CanExecute(object parameter)
		{
			return true;
		}

		public void OnCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}

		public abstract void Execute(object parameter);
	}
}
