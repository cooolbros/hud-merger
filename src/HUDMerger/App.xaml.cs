using System;
using System.Windows;
using HUDMerger.ViewModels;

namespace HUDMerger;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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
