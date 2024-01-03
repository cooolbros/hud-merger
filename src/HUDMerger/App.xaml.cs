using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using HUDMerger.Extensions;
using HUDMerger.Models;
using HUDMerger.ViewModels;
using Microsoft.Win32;
using VDF;
using VDF.Models;

namespace HUDMerger;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	[GeneratedRegex(@"[/\\]+")]
	public static partial Regex PathSeparatorRegex();

	public Lazy<Settings> Settings = new(() =>
	{
		string? teamFortress2Folder = null;
		string? language = null;

		if (!string.IsNullOrEmpty(HUDMerger.Properties.Settings.Default.TeamFortress2Folder))
		{
			teamFortress2Folder = HUDMerger.Properties.Settings.Default.TeamFortress2Folder;
		}
		else
		{
			try
			{
				// "C:\Program Files (x86)\Steam"
				string? installPath = (
					Registry.GetValue($"HKEY_LOCAL_MACHINE\\Software\\{(Environment.Is64BitProcess ? "Wow6432Node\\" : "")}Valve\\Steam", "InstallPath", "") is string str
						? str
						: null
				) ?? throw new Exception();

				try
				{
					KeyValues libraryFolder = VDFSerializer
						.Deserialize(File.ReadAllText(Path.Join(installPath, "steamapps\\libraryfolders.vdf")))
						.Header("libraryfolders")
						.First((keyValue) =>
							keyValue.Value is KeyValues indexValues && indexValues.Any((kv) => kv.Key.Equals("apps") && kv.Value is KeyValues appsKeyValues && appsKeyValues.Any((kv) => kv.Key == "440"))
						)
						.Value;

					string libraryFolderPath = libraryFolder.First((kv) => kv.Key.Equals("path", StringComparison.OrdinalIgnoreCase) && kv.Value is string str).Value;

					string libraryFolderTeamFortress2Path = $"{libraryFolderPath.Replace("\\\\", "\\")}\\steamapps\\common\\Team Fortress 2";
					if (Directory.Exists(libraryFolderTeamFortress2Path))
					{
						teamFortress2Folder = libraryFolderTeamFortress2Path;
					}
				}
				catch (Exception)
				{
					teamFortress2Folder = null;
				}

				try
				{
					string appManifestPath = Path.Join(installPath, "steamapps\\appmanifest_440.acf");

					KeyValues userConfig = VDFSerializer
						.Deserialize(File.ReadAllText(Path.Join(installPath, "steamapps\\appmanifest_440.acf")))
						.Header("AppState")
						.First((kv) => kv.Key.Equals("UserConfig", StringComparison.OrdinalIgnoreCase) && kv.Value is KeyValues)
						.Value;

					string userConfigLanguage = userConfig
						.First((kv) => kv.Key.Equals("language", StringComparison.OrdinalIgnoreCase) && kv.Value is string)
						.Value;

					language = userConfigLanguage;
				}
				catch (Exception)
				{
					language = null;
				}
			}
			catch (Exception)
			{
				// Registry Exception
				teamFortress2Folder ??= null;
				language ??= null;
			}
		}

		return new Settings
		{
			TeamFortress2Folder = teamFortress2Folder ?? "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2",
			Language = language ?? "english"
		};
	});

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		MainWindow = new MainWindow
		{
			DataContext = new MainWindowViewModel()
		};

		MainWindow.Show();
	}
}
