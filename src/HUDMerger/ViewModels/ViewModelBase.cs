using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HUDMerger.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
{
	private bool _disposed;
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

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
