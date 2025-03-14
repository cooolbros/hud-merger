﻿using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using HUDMerger.Core.Models;

namespace HUDMerger.Core.ViewModels;

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
