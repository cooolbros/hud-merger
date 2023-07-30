using System;
using System.Collections.Generic;

namespace HUDMerger.ViewModels
{
	public class SourceHUDPanelsListViewModel : ViewModelBase
	{
		private string _searchText;
		public string SearchText
		{
			get => _searchText;
			set
			{
				_searchText = value;
				OnPropertyChanged(nameof(SearchText));
				UpdateHUDPanelsVisibility();
			}
		}

		public IEnumerable<HUDPanelViewModel> HUDPanels { get; }

		public SourceHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
		{
			HUDPanels = hudPanelViewModels;
		}

		private void UpdateHUDPanelsVisibility()
		{
			foreach (HUDPanelViewModel hudPanelViewModel in HUDPanels)
			{
				hudPanelViewModel.Visible = hudPanelViewModel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}
