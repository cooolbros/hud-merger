using System;
using System.Text.RegularExpressions;
using System.Windows;
using HUDMerger.ViewModels;

namespace HUDMerger;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	[GeneratedRegex(@"[/\\]+")]
	public static partial Regex PathSeparatorRegex();

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
