using System;
using System.Windows.Input;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels;

public class HUDPanelViewModel : ViewModelBase
{
	public readonly HUDPanel HUDPanel;

	public string Name => HUDPanel.Name;

	private bool _selected;
	public bool Selected
	{
		get => _selected;
		set
		{
			_selected = value;
			OnPropertyChanged();
		}
	}

	public ICommand ToggleSelectedCommand { get; }

	public HUDPanelViewModel(HUDPanel hudPanel)
	{
		HUDPanel = hudPanel;
		ToggleSelectedCommand = new RelayCommand(() => Selected = !Selected);
	}
}
