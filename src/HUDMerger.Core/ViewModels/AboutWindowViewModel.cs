using System;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace HUDMerger.Core.ViewModels;

public class AboutWindowViewModel : ViewModelBase
{
	public ICommand OpenGithubCommand { get; }
	public ICommand OpenTeamFortressTVCommand { get; }

	public AboutWindowViewModel()
	{
		OpenGithubCommand = new RelayCommand(OpenGithub);
		OpenTeamFortressTVCommand = new RelayCommand(OpenTeamFortressTV);
	}

	private void OpenGithub()
	{
		Process.Start("explorer", "https://github.com/cooolbros/hud-merger");
	}

	private void OpenTeamFortressTV()
	{
		Process.Start("explorer", "https://www.teamfortress.tv/60220/hud-merger");
	}
}
