using System;
using System.Windows;

namespace HUDMerger
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			Properties.Settings.Default.Upgrade();
			Properties.Settings.Default.Save();

			// Updater
			bool download = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up;
			bool extract = Properties.Settings.Default.Extract_required_TF2_HUD_files_on_startup;
			Updater.Update(download, extract);
		}
	}
}
