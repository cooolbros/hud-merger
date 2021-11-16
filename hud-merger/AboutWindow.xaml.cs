using System;
using System.Diagnostics;
using System.Windows;

namespace HUDMerger
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
		}

		private void Open_Source(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer", "https://github.com/cooolbros/hud-merger");
		}
	}
}