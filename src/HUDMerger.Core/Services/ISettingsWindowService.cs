using System;
using HUDMerger.Core.ViewModels;

namespace HUDMerger.Core.Services;

public interface ISettingsWindowService
{
	public void Show(SettingsWindowViewModel settingsWindowViewModel);
}
