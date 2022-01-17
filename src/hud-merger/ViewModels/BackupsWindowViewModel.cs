using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels
{
	public class BackupsWindowViewModel : ViewModelBase
	{
		private ObservableCollection<HUDBackupViewModel> _hudBackups;
		public ObservableCollection<HUDBackupViewModel> HUDBackups
		{
			get => _hudBackups;
			set
			{
				_hudBackups = value;
				OnPropertyChanged(nameof(HUDBackups));
			}
		}

		private string _hudNameLabel = "HUD Name";
		/// <summary>
		/// HUD Name Label
		/// </summary>
		public string HUDNameLabel
		{
			get => _hudNameLabel;
			set
			{
				_hudNameLabel = value;
				OnPropertyChanged(nameof(HUDNameLabel));
			}
		}

		private string _fileNameLabel = "File Name";
		/// <summary>
		///	File Name Label
		/// </summary>
		public string FileNameLabel
		{
			get => _fileNameLabel;
			set
			{
				_fileNameLabel = value;
				OnPropertyChanged(nameof(FileNameLabel));
			}
		}

		private string _creationTimeLabel = "Creation Time";
		/// <summary>
		/// Creation Time Label
		/// </summary>
		public string CreationTimeLabel
		{
			get => _creationTimeLabel;
			set
			{
				_creationTimeLabel = value;
				OnPropertyChanged(nameof(CreationTimeLabel));
			}
		}

		/// <summary>
		/// Arrow Pointing Up
		/// </summary>
		private const string UpArrow = "\u25B2";

		/// <summary>
		/// Arrow Pointing Down
		/// </summary>
		private const string DownArrow = "\u25BC";

		private bool SortAscending = true;
		private string LastClickedLabel;

		public ICommand HUDNameClickCommand { get; }
		public ICommand FileNameClickCommand { get; }
		public ICommand CreationTimeClickCommand { get; }


		public ICommand DeleteBackupCommand { get; }

		public BackupsWindowViewModel()
		{
			string backupsDirectory = HUDBackupManager.BackupDirectory;

#if DEBUG
			// Visual Studio
			if (!Directory.Exists(backupsDirectory))
			{
				backupsDirectory = $"../../../{backupsDirectory}";
			}
#endif

			_hudBackups = new ObservableCollection<HUDBackupViewModel>(new DirectoryInfo(HUDBackupManager.BackupDirectory)
			.GetDirectories()
			.SelectMany((DirectoryInfo info) => info.GetFiles().Select(fileInfo => new HUDBackupViewModel(new HUDBackup(info.Name, fileInfo)))));

			HUDNameClickCommand = new RelayCommand(HUDName_Click);
			FileNameClickCommand = new RelayCommand(FileName_Click);
			CreationTimeClickCommand = new RelayCommand(CreationTime_Click);

			DeleteBackupCommand = new RelayCommand<HUDBackupViewModel>((HUDBackupViewModel backup) =>
			{
				MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete {backup.Name}?", "Delete Backup", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					File.Delete(backup.FullName);
					_hudBackups.Remove(backup);
				}
			});
		}

		private void HUDName_Click()
		{
			UpdateHUDBackupsItemsControl(nameof(HUDNameLabel), hudBackupViewModel => hudBackupViewModel.HUDName);
			HUDNameLabel = $"HUD Name {(SortAscending ? UpArrow : DownArrow)}";
			FileNameLabel = "File Name";
			CreationTimeLabel = "Creation Time";
		}

		private void FileName_Click()
		{
			UpdateHUDBackupsItemsControl(nameof(FileNameLabel), hudBackupViewModel => hudBackupViewModel.Name);
			HUDNameLabel = "HUD Name";
			FileNameLabel = $"File Name {(SortAscending ? UpArrow : DownArrow)}";
			CreationTimeLabel = "Creation Time";
		}

		private void CreationTime_Click()
		{
			UpdateHUDBackupsItemsControl(nameof(CreationTimeLabel), hudBackupViewModel => hudBackupViewModel.CreationTime);
			HUDNameLabel = "HUD Name";
			FileNameLabel = "File Name";
			CreationTimeLabel = $"Creation Time {(SortAscending ? UpArrow : DownArrow)}";
		}

		private void UpdateHUDBackupsItemsControl(string clickedLabel, Func<HUDBackupViewModel, dynamic> keySelector)
		{
			if (LastClickedLabel == clickedLabel)
			{
				SortAscending = !SortAscending;
			}
			LastClickedLabel = clickedLabel;
			HUDBackups = new ObservableCollection<HUDBackupViewModel>(SortAscending ? HUDBackups.OrderBy(keySelector).ToArray() : HUDBackups.OrderByDescending(keySelector));
		}
	}
}
