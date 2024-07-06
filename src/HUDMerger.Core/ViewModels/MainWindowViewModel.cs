using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Discord;
using HUDMerger.Commands;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;

namespace HUDMerger.Core.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private static readonly OpenFolderDialog OpenFolderDialog = new()
	{
		InitialDirectory = Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "tf\\custom\\")
	};

	private static readonly Channel<(string? sourceName, string? targetName)> DiscordChannel = Channel.CreateBounded<(string? sourceName, string? targetName)>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

	// File
	public ICommand LoadSourceHUDCommand { get; }
	public ICommand LoadTargetHUDCommand { get; }
	public ICommand ShowSettingsWindowCommand { get; }
	public ICommand QuitCommand { get; }

	// About
	public ICommand ShowAboutWindowCommand { get; }

	public readonly List<HUDPanelViewModel> HUDPanelViewModels = [];

	private HUD? _sourceHUD;
	public HUD? SourceHUD
	{
		get => _sourceHUD;
		set
		{
			_sourceHUD = value;
			OnPropertyChanged();
			DiscordChannel.Writer.TryWrite((SourceHUD?.Name, TargetHUD?.Name));
		}
	}

	private HUD? _targetHUD;
	public HUD? TargetHUD
	{
		get => _targetHUD;
		set
		{
			_targetHUD = value;
			OnPropertyChanged();
			DiscordChannel.Writer.TryWrite((SourceHUD?.Name, TargetHUD?.Name));
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
		ShowSettingsWindowCommand = new RelayCommand(ShowSettingsWindow);
		QuitCommand = new RelayCommand(Application.Current.Shutdown);

		ShowAboutWindowCommand = new RelayCommand(ShowAboutWindow);

		SourceHUDInfoViewModel = new HUDInfoViewModel("from", SourceHUD);
		TargetHUDInfoViewModel = new HUDInfoViewModel("to", TargetHUD);

		_sourceHUDPanelsListViewModel = new SelectHUDViewModel(LoadSourceHUDCommand);
		_targetHUDPanelsListViewModel = new SelectHUDViewModel(LoadTargetHUDCommand);

		MergeCommand = new MergeCommand(this);

		ChannelReader<(string? sourceName, string? targetName)> reader = DiscordChannel.Reader;
		Task.Run(async () => await DiscordRichPresence(reader));
	}

	private void ShowSettingsWindow()
	{
		using SettingsWindowViewModel settingsWindowViewModel = new();

		SettingsWindow settingsWindow = new()
		{
			DataContext = settingsWindowViewModel,
			Owner = Application.Current.MainWindow
		};

		void OnClose(object? sender, EventArgs args)
		{
			settingsWindowViewModel.Close -= OnClose;
			settingsWindow.Close();
		}

		settingsWindowViewModel.Close += OnClose;

		settingsWindow.Show();
	}

	private void ShowAboutWindow()
	{
		using AboutWindowViewModel aboutWindowViewModel = new();

		AboutWindow aboutWindow = new()
		{
			DataContext = aboutWindowViewModel,
			Owner = Application.Current.MainWindow
		};

		aboutWindow.Show();
	}

	private void LoadSourceHUD()
	{
		if (OpenFolderDialog.ShowDialog(Application.Current.MainWindow) == true)
		{
			SourceHUD = new HUD(OpenFolderDialog.FolderName);
			SourceHUDInfoViewModel.HUD = SourceHUD;

			HUDPanelViewModels.Clear();

			foreach (HUDPanel hudPanel in SourceHUD.Panels)
			{
				HUDPanelViewModel hudPanelViewModel = new(hudPanel);
				hudPanelViewModel.PropertyChanged += HudPanelViewModel_PropertyChanged;
				HUDPanelViewModels.Add(hudPanelViewModel);
			}

			SourceHUDPanelsListViewModel?.Dispose();
			SourceHUDPanelsListViewModel = new SourceHUDPanelsListViewModel(HUDPanelViewModels);

			if (TargetHUDPanelsListViewModel is TargetHUDPanelsListViewModel targetHUDPanelsListViewModel)
			{
				targetHUDPanelsListViewModel.HUDPanelsCollectionView.Refresh();
			}
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

	private void HudPanelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(HUDPanelViewModel.Selected) && TargetHUDPanelsListViewModel is TargetHUDPanelsListViewModel targetHUDPanelsListViewModel)
		{
			targetHUDPanelsListViewModel.HUDPanelsCollectionView.Refresh();
		}
	}

	private static async Task DiscordRichPresence(ChannelReader<(string? sourceName, string? targetName)> reader)
	{
		Discord.Discord discord = new(1188809773664190524, (ulong)CreateFlags.Default);

		try
		{
			Activity activity = new()
			{
				Type = ActivityType.Streaming,
				Details = "Merging HUDs",
				Timestamps = new ActivityTimestamps
				{
					Start = DateTimeOffset.Now.ToUnixTimeSeconds()
				},
				Assets = new()
				{
					LargeImage = "hud-merger"
				},
			};

			while (true)
			{
				if (reader.TryRead(out var item))
				{
					activity.Details = item switch
					{
						(null, null) => "Merging HUDs",
						(string sourceName, null) => $"Merging from {sourceName}",
						(null, string targetName) => $"Merging into {targetName}",
						(string sourceName, string targetName) => $"Merging from {sourceName} into {targetName}"
					};
				}

				discord.GetActivityManager().UpdateActivity(activity, (result) => Console.WriteLine(result));
				discord.RunCallbacks();
				await Task.Delay(1000 / 60);
			}
		}
		finally
		{
			discord.Dispose();
		}
	}
}
