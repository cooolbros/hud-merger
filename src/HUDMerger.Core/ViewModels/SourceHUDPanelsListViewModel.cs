using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HUDMerger.Core.ViewModels;

public class SourceHUDPanelsListViewModel : ViewModelBase
{
	private readonly IEnumerable<HUDPanelViewModel> HUDPanelViewModels;

	private ObservableCollection<HUDPanelViewModel> _hudPanelsCollectionView;
	public ObservableCollection<HUDPanelViewModel> HUDPanelsCollectionView
	{
		get => _hudPanelsCollectionView;
		private set
		{
			_hudPanelsCollectionView = value;
			OnPropertyChanged();
		}
	}

	private string _searchText = "";
	public string SearchText
	{
		get => _searchText;
		set
		{
			_searchText = value;
			OnPropertyChanged();
			Refresh();
		}
	}

	public SourceHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
	{
		HUDPanelViewModels = hudPanelViewModels;
		_hudPanelsCollectionView = new ObservableCollection<HUDPanelViewModel>(
			HUDPanelViewModels.Where((hudPanelViewModel) =>
			{
				return hudPanelViewModel.HUDPanel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
					|| hudPanelViewModel.HUDPanel.Main.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
			})
		);
	}

	public void Refresh()
	{
		HUDPanelsCollectionView = new ObservableCollection<HUDPanelViewModel>(
			HUDPanelViewModels.Where((hudPanelViewModel) =>
			{
				return hudPanelViewModel.HUDPanel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
					|| hudPanelViewModel.HUDPanel.Main.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
			})
		);
	}
}
