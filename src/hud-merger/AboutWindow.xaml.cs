using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

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

		private void Open_Github(object sender, MouseButtonEventArgs e)
		{
			Process.Start("explorer", "https://github.com/cooolbros/hud-merger");
		}

		private void Open_TeamFortressTV(object sender, MouseButtonEventArgs e)
		{
			Process.Start("explorer", "https://www.teamfortress.tv/60220/hud-merger");
		}
	}
}
