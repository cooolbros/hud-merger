using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace HUDMerger.Core.ViewModels;

public class SourceHUDPanelsListViewModel : ViewModelBase
{
	public ICollectionView HUDPanelsCollectionView { get; }

	private string _searchText = "";
	public string SearchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
			OnPropertyChanged();
			HUDPanelsCollectionView.Refresh();
		}
	}

	public SourceHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
	{
		HUDPanelsCollectionView = new CollectionViewSource { Source = hudPanelViewModels }.View;
		HUDPanelsCollectionView.Filter = (object obj) =>
		{
			if (obj is HUDPanelViewModel hudPanelViewModel)
			{
				return hudPanelViewModel.HUDPanel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
					|| hudPanelViewModel.HUDPanel.Main.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		};
	}
}
