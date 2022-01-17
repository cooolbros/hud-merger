using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels
{
	public class HUDBackupViewModel : ViewModelBase
	{
		private readonly HUDBackup _backup;

		public string HUDName => _backup.HUDName;
		public string Name => _backup.Name;
		public string FullName => _backup.FullName;
		public DateTime CreationTime => _backup.CreationTime;

		public ICommand ShowBackupCommand { get; }

		public HUDBackupViewModel(HUDBackup backup)
		{
			_backup = backup;
			ShowBackupCommand = new RelayCommand(() =>
			{
				Process.Start("explorer", $"/select,\"{_backup.FullName}\"");
			});
		}
	}
}
