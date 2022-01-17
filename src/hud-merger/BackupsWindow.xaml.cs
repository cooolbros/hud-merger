using System;
using System.Windows;
using HUDMerger.ViewModels;

namespace HUDMerger
{
	/// <summary>
	/// Interaction logic for BackupsWindow.xaml
	/// </summary>
	public partial class BackupsWindow : Window
	{
		public BackupsWindow()
		{
			InitializeComponent();
			DataContext = new BackupsWindowViewModel();
		}
	}
}
