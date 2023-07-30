using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HUDMerger;

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
			await Task.WhenAll(Updater.Update(true, true));
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
