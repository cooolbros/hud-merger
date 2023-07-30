using System;
using System.Collections.Generic;

namespace HUDMerger.ViewModels
{
	public class TargetHUDPanelsListViewModel : ViewModelBase
	{
		public IEnumerable<HUDPanelViewModel> HUDPanels { get; }

		public TargetHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels)
		{
			HUDPanels = hudPanelViewModels;
		}
	}
}
