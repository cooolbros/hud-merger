using System;
using System.Windows;
using HUDMerger.Core.Services;
using HUDMerger.Core.ViewModels;

namespace HUDMerger.Services;

public class SettingsWindowService : ISettingsWindowService
{
	public void Show(SettingsWindowViewModel settingsWindowViewModel)
	{
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
}
