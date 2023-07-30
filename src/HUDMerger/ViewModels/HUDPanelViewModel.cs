using System;
using System.Windows.Input;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels;

public class HUDPanelViewModel : ViewModelBase
{
	public readonly HUDPanel HUDPanel;

	public string Name => HUDPanel.Name;

	private bool _visible;
	public bool Visible
	{
		get => _visible;
		set
		{
			_visible = value;
			OnPropertyChanged(nameof(Visible));
		}
	}

	private bool _armed;
	public bool Armed
	{
		get => _armed;
		set
		{
			_armed = value;
			OnPropertyChanged(nameof(Armed));
		}
	}

	public ICommand ToggleSelectedCommand { get; }

	public HUDPanelViewModel(HUDPanel hudPanel)
	{
		HUDPanel = hudPanel;
		Visible = true;
		ToggleSelectedCommand = new RelayCommand(() => Armed = !Armed);
	}

	public bool TestPanel(HUD hud)
	{
		return hud.TestPanel(HUDPanel);
	}
}
