using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace hud_merger
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		private async void UpdateFilesNowButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateStatus.Content = "";
			UpdateStatus.Foreground = Brushes.Black;
			try
			{
				List<Task> tasks = Updater.Update(true, true);
				await Task.WhenAll(tasks);
				UpdateStatus.Content = $"Last updated on {DateTime.Now}";
			}
			catch (Exception error)
			{
				UpdateStatus.Content = error.Message;
				UpdateStatus.Foreground = Brushes.Red;
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.Reload();
			Close();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.Save();
			Close();
		}
	}
}
