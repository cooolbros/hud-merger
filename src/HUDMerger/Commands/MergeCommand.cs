using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using HUDMerger.Models;
using HUDMerger.ViewModels;

namespace HUDMerger.Commands;

public class MergeCommand : CommandBase
{
	private readonly MainWindowViewModel _mainWindowViewModel;

	public MergeCommand(MainWindowViewModel mainWindowViewModel)
	{
		_mainWindowViewModel = mainWindowViewModel;
		_mainWindowViewModel.PropertyChanged += _mainWindowViewModel_PropertyChanged;
	}

	private void _mainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MainWindowViewModel.SourceHUD) || e.PropertyName == nameof(MainWindowViewModel.TargetHUD))
		{
			OnCanExecuteChanged();
		}
	}

	public override bool CanExecute(object parameter)
	{
		return _mainWindowViewModel.SourceHUD != null && _mainWindowViewModel.TargetHUD != null && base.CanExecute(parameter);
	}

	public override void Execute(object parameter)
	{
		try
		{
			if (Utilities.PathContainsPath(Path.Join(Properties.Settings.Default.Team_Fortress_2_Folder, "tf\\custom"), _mainWindowViewModel.TargetHUD.FolderPath) && Process.GetProcessesByName("hl2").Any())
			{
				MessageBox.Show("HL2 process open, cannot merge!", "HL2 Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			_mainWindowViewModel.TargetHUD.Merge(_mainWindowViewModel.SourceHUD, MainWindowViewModel.HUDPanels.Where(hudPanelViewModel => hudPanelViewModel.Armed).Select(hudPanelViewModel => hudPanelViewModel.HUDPanel).ToArray());
			MessageBox.Show("Done!");
		}
		catch (Exception e)
		{
			List<string> paragraphs = new List<string>
				{
					e.Message,
				};

			MessageBox.Show(String.Join("\r\n\r\n", paragraphs), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		_mainWindowViewModel.PropertyChanged -= _mainWindowViewModel_PropertyChanged;
	}
}
