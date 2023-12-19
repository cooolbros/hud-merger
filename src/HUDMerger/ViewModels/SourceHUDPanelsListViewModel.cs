using System;
using System.Collections.Generic;

namespace HUDMerger.ViewModels;

public class SourceHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels) : ViewModelBase
{
	public IEnumerable<HUDPanelViewModel> HUDPanels { get; } = hudPanelViewModels;

	private string _searchText = "";
	public string SearchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
			OnPropertyChanged();
			UpdateHUDPanelsVisibility();
		}
	}

	private void UpdateHUDPanelsVisibility()
	{
		foreach (HUDPanelViewModel hudPanelViewModel in HUDPanels)
		{
			hudPanelViewModel.Visible = hudPanelViewModel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
		}
	}
}
