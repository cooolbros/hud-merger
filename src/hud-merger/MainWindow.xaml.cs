using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HUDMerger.Models;

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
			Task.WhenAll(Updater.Update(download, extract)).ContinueWith((Task task) =>
			{
				if (task.Exception != null)
				{
					foreach (Exception error in task.Exception.InnerExceptions)
					{
						System.Diagnostics.Debug.WriteLine($"({error.GetType().Name}): {error.Message}");
					}
				}
			});
		}
	}
}
