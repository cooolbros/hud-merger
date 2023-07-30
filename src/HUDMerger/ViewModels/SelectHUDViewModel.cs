using System;
using System.Windows.Input;

namespace HUDMerger.ViewModels
{
	public class SelectHUDViewModel : ViewModelBase
	{
		public ICommand SelectHUDCommand { get; }

		public SelectHUDViewModel(ICommand selectHUDCommand)
		{
			SelectHUDCommand = selectHUDCommand;
		}
	}
}
