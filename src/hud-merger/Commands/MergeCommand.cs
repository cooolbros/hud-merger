using System;
using System.Linq;
using System.Windows;
using HUDMerger.ViewModels;

namespace HUDMerger.Commands
{
	public class MergeCommand : CommandBase
	{
		private readonly MainWindowViewModel _mainWindowViewModel;

		public MergeCommand(MainWindowViewModel mainWindowViewModel)
		{
			_mainWindowViewModel = mainWindowViewModel;
		}

		public override bool CanExecute(object parameter)
		{
			return _mainWindowViewModel.SourceHUD != null && _mainWindowViewModel.TargetHUD != null && base.CanExecute(parameter);
		}

		public override void Execute(object parameter)
		{
			try
			{
				_mainWindowViewModel.TargetHUD.Merge(_mainWindowViewModel.SourceHUD, _mainWindowViewModel.HUDPanels.Where(hudPanelViewModel => hudPanelViewModel.Armed).Select(hudPanelViewModel => hudPanelViewModel.HUDPanel).ToArray());
				MessageBox.Show("Done!");
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
