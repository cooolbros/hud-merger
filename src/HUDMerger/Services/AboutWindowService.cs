using System;
using System.Windows;
using HUDMerger.Core.Services;
using HUDMerger.Core.ViewModels;

namespace HUDMerger.Services;

public class AboutWindowService : IAboutWindowService
{
	public void Show(AboutWindowViewModel aboutWindowViewModel)
	{
		AboutWindow aboutWindow = new()
		{
			DataContext = aboutWindowViewModel,
			Owner = Application.Current.MainWindow
		};

		aboutWindow.Show();
	}
}
