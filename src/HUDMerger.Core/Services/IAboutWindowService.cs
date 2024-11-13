using System;
using HUDMerger.Core.ViewModels;

namespace HUDMerger.Core.Services;

public interface IAboutWindowService
{
	public void Show(AboutWindowViewModel aboutWindowViewModel);
}
