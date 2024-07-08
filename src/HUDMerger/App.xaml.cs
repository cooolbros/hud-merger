using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using HUDMerger.Core.Extensions;
using HUDMerger.Core.Models;
using HUDMerger.Core.ViewModels;
using HUDMerger.Services;
using Microsoft.Win32;
using VDF;
using VDF.Models;

namespace HUDMerger;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		HUDMerger.Properties.Settings.Default.Upgrade();
		HUDMerger.Properties.Settings.Default.Save();

		SettingsService settingsService = new();

		MainWindow = new MainWindow
		{
			DataContext = new MainWindowViewModel(
				settingsService,
				new FolderPickerService(settingsService),
				new SettingsWindowService(),
				new AboutWindowService(),
				new MessageBoxService()
			)
		};

		MainWindow.Show();
	}
}
