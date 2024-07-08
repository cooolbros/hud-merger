using System;
using System.Windows;
using HUDMerger.Core.Services;

namespace HUDMerger.Services;

public class QuitService : IQuitService
{
	public void Quit()
	{
		Application.Current.Shutdown();
	}
}
