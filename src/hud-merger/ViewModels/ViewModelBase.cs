using System;
using System.ComponentModel;

namespace HUDMerger.ViewModels
{
	public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public virtual void Dispose()
		{
		}
	}
}
