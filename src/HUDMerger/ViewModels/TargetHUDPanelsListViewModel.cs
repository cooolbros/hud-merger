using System;
using System.Collections.Generic;

namespace HUDMerger.ViewModels;

public class TargetHUDPanelsListViewModel(IEnumerable<HUDPanelViewModel> hudPanelViewModels) : ViewModelBase
{
	public IEnumerable<HUDPanelViewModel> HUDPanels { get; } = hudPanelViewModels;
}
