using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using HUDMerger.Commands;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private static FolderBrowserDialog FolderBrowserDialog = new FolderBrowserDialog
		{
			SelectedPath = Path.Join(Properties.Settings.Default.Team_Fortress_2_Folder, "tf\\custom\\")
		};

		public ICommand NewSourceHUDCommand { get; }
		public ICommand NewTargetHUDCommand { get; }

		public ICommand ShowBackupsWindowCommand { get; }
		public ICommand ShowSettingsWindowCommand { get; }
		public ICommand ShowAboutWindowCommand { get; }

		public ICommand QuitCommand { get; }

		public static HUDPanelViewModel[] HUDPanels;

		private HUD _sourceHUD;
		public HUD SourceHUD
		{
			get => _sourceHUD;
			set
			{
				_sourceHUD = value;
				OnPropertyChanged(nameof(SourceHUD));
			}
		}

		private HUD _targetHUD;
		public HUD TargetHUD
		{
			get => _targetHUD;
			set
			{
				_targetHUD = value;
				OnPropertyChanged(nameof(TargetHUD));
			}
		}

		public HUDInfoViewModel SourceHUDInfoViewModel { get; }
		public HUDInfoViewModel TargetHUDInfoViewModel { get; }

		private ViewModelBase _sourceHUDPanelsListViewModel;
		public ViewModelBase SourceHUDPanelsListViewModel
		{
			get => _sourceHUDPanelsListViewModel;
			set
			{
				_sourceHUDPanelsListViewModel = value;
				OnPropertyChanged(nameof(SourceHUDPanelsListViewModel));
			}
		}

		private ViewModelBase _targetHUDPanelsListViewModel;
		public ViewModelBase TargetHUDPanelsListViewModel
		{
			get => _targetHUDPanelsListViewModel;
			set
			{
				_targetHUDPanelsListViewModel = value;
				OnPropertyChanged(nameof(TargetHUDPanelsListViewModel));
			}
		}

		public MergeCommand MergeCommand { get; }

		public MainWindowViewModel()
		{
			HUDPanels = JsonSerializer
			.Deserialize<HUDPanel[]>(File.OpenRead("Resources\\Panels.json"))
			.Select<HUDPanel, HUDPanelViewModel>(hudPanel => new HUDPanelViewModel(hudPanel))
			.ToArray();

			NewSourceHUDCommand = new RelayCommand(NewSourceHUD);
			NewTargetHUDCommand = new RelayCommand(NewTargetHUD);
			ShowBackupsWindowCommand = new RelayCommand(() => ShowWindow(new BackupsWindow()));
			ShowSettingsWindowCommand = new RelayCommand(() => ShowWindow(new SettingsWindow()));
			ShowAboutWindowCommand = new RelayCommand(() => ShowWindow(new AboutWindow()));
			QuitCommand = new RelayCommand(System.Windows.Application.Current.Shutdown);

			SourceHUDInfoViewModel = new HUDInfoViewModel("from", SourceHUD);
			TargetHUDInfoViewModel = new HUDInfoViewModel("to", TargetHUD);

			SourceHUDPanelsListViewModel = new SelectHUDViewModel(NewSourceHUDCommand);
			TargetHUDPanelsListViewModel = new SelectHUDViewModel(NewTargetHUDCommand);

			MergeCommand = new MergeCommand(this);
		}

		private void ShowWindow(Window window)
		{
			window.Owner ??= System.Windows.Application.Current.MainWindow;
			window.Show();
		}

		private void NewSourceHUD()
		{
			DialogResult result = FolderBrowserDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				foreach (HUDPanelViewModel hudPanelViewModel in HUDPanels)
				{
					hudPanelViewModel.Armed = false;
				}
				SourceHUD = new HUD(FolderBrowserDialog.SelectedPath);
				SourceHUDInfoViewModel.HUD = SourceHUD;

				SourceHUDPanelsListViewModel?.Dispose();
				SourceHUDPanelsListViewModel = new SourceHUDPanelsListViewModel(HUDPanels.Where(hudPanel => hudPanel.TestPanel(SourceHUD)));
			}
		}

		/// <summary>
		///
		/// </summary>
		private void NewTargetHUD()
		{
			DialogResult result = FolderBrowserDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				TargetHUD = new HUD(FolderBrowserDialog.SelectedPath);
				TargetHUDInfoViewModel.HUD = TargetHUD;

				TargetHUDPanelsListViewModel?.Dispose();
				TargetHUDPanelsListViewModel = new TargetHUDPanelsListViewModel(HUDPanels);
			}
		}
	}
}
