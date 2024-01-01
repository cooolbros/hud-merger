using System;
using System.Windows;

namespace HUDMerger;

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
	}
}
