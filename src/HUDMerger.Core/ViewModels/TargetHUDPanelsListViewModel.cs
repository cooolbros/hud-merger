using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HUDMerger.Core.ViewModels;

public class TargetHUDPanelsListViewModel : ViewModelBase
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

	public TargetHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
	{
		HUDPanelViewModels = hudPanelViewModels;
		_hudPanelsCollectionView = new ObservableCollection<HUDPanelViewModel>(
			HUDPanelViewModels.Where((hudPanelViewModel) =>
			{
				return hudPanelViewModel.Selected;
			})
		);
	}

	public void Refresh()
	{
		HUDPanelsCollectionView = new ObservableCollection<HUDPanelViewModel>(
			HUDPanelViewModels.Where((hudPanelViewModel) =>
			{
				return hudPanelViewModel.Selected;
			})
		);
	}
}
