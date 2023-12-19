using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HUDMerger.Commands;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;

namespace HUDMerger.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private static readonly OpenFolderDialog OpenFolderDialog = new()
	{
		FolderName = Path.Join(Properties.Settings.Default.Team_Fortress_2_Folder, "tf\\custom\\")
	};

	// File
	public ICommand LoadSourceHUDCommand { get; }
	public ICommand LoadTargetHUDCommand { get; }
	public ICommand ShowSettingsWindowCommand { get; }
	public ICommand QuitCommand { get; }

	// About
	public ICommand ShowAboutWindowCommand { get; }

	private readonly List<HUDPanelViewModel> HUDPanelViewModels = [];

	public HUD? SourceHUD;
	public HUD? TargetHUD;

	public HUDInfoViewModel SourceHUDInfoViewModel { get; }
	public HUDInfoViewModel TargetHUDInfoViewModel { get; }

	private ViewModelBase _sourceHUDPanelsListViewModel;
	public ViewModelBase SourceHUDPanelsListViewModel
	{
		get => _sourceHUDPanelsListViewModel;
		set
		{
			_sourceHUDPanelsListViewModel = value;
			OnPropertyChanged();
		}
	}

	private ViewModelBase _targetHUDPanelsListViewModel;
	public ViewModelBase TargetHUDPanelsListViewModel
	{
		get => _targetHUDPanelsListViewModel;
		set
		{
			_targetHUDPanelsListViewModel = value;
			OnPropertyChanged();
		}
	}

	public MergeCommand MergeCommand { get; }

	public MainWindowViewModel()
	{
		LoadSourceHUDCommand = new RelayCommand(LoadSourceHUD);
		LoadTargetHUDCommand = new RelayCommand(LoadTargetHUD);
		ShowSettingsWindowCommand = new RelayCommand(() => ShowWindow(new SettingsWindow()));
		QuitCommand = new RelayCommand(Application.Current.Shutdown);

		ShowAboutWindowCommand = new RelayCommand(() => ShowWindow(new AboutWindow()));

		SourceHUDInfoViewModel = new HUDInfoViewModel("from", SourceHUD);
		TargetHUDInfoViewModel = new HUDInfoViewModel("to", TargetHUD);

		_sourceHUDPanelsListViewModel = new SelectHUDViewModel(LoadSourceHUDCommand);
		_targetHUDPanelsListViewModel = new SelectHUDViewModel(LoadTargetHUDCommand);

		MergeCommand = new MergeCommand(this);
	}

	private static void ShowWindow(Window window)
	{
		window.Owner ??= Application.Current.MainWindow;
		window.Show();
	}

	private void LoadSourceHUD()
	{
		if (OpenFolderDialog.ShowDialog(Application.Current.MainWindow) == true)
		{
			SourceHUD = new HUD(OpenFolderDialog.FolderName);
			SourceHUDInfoViewModel.HUD = SourceHUD;

			HUDPanelViewModels.Clear();
			HUDPanelViewModels.AddRange(SourceHUD.Panels.Select((panel) => new HUDPanelViewModel(panel)));

			SourceHUDPanelsListViewModel?.Dispose();
			SourceHUDPanelsListViewModel = new SourceHUDPanelsListViewModel(HUDPanelViewModels);
		}
	}

	private void LoadTargetHUD()
	{
		if (OpenFolderDialog.ShowDialog(Application.Current.MainWindow) == true)
		{
			TargetHUD = new HUD(OpenFolderDialog.FolderName);
			TargetHUDInfoViewModel.HUD = TargetHUD;

			TargetHUDPanelsListViewModel?.Dispose();
			TargetHUDPanelsListViewModel = new TargetHUDPanelsListViewModel(HUDPanelViewModels);
		}
	}
}
