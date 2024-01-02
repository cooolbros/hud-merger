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
