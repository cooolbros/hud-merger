using System;
using System.Windows.Input;

namespace HUDMerger.Commands;

public abstract class CommandBase : ICommand, IDisposable
{
	private bool _disposed;
	public event EventHandler? CanExecuteChanged;

	public virtual bool CanExecute(object? parameter)
	{
		return true;
	}

	public void OnCanExecuteChanged()
	{
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	public abstract void Execute(object? parameter);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
		}

		_disposed = true;
	}
}
