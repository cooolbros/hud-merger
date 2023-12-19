using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace HUDMerger.ViewModels;

public class TargetHUDPanelsListViewModel : ViewModelBase
{
	public ICollectionView HUDPanelsCollectionView { get; }

	public TargetHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
	{
		HUDPanelsCollectionView = new CollectionViewSource { Source = hudPanelViewModels }.View;
		HUDPanelsCollectionView.Filter = (object obj) =>
		{
			return obj is HUDPanelViewModel hudPanelViewModel && hudPanelViewModel.Selected;
		};
	}
}
