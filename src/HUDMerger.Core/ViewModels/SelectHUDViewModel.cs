using System;
using System.Windows.Input;

namespace HUDMerger.Core.ViewModels;

public class SelectHUDViewModel(ICommand selectHUDCommand) : ViewModelBase
{
	public ICommand SelectHUDCommand { get; } = selectHUDCommand;
}
