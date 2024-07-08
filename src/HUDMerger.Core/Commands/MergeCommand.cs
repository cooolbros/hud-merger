using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using HUDMerger.Core.Models;
using HUDMerger.Core.Services;
using HUDMerger.Core.ViewModels;

namespace HUDMerger.Core.Commands;

public class MergeCommand : CommandBase
{
	private bool _disposed;
	private readonly MainWindowViewModel _mainWindowViewModel;
	private readonly IMessageBoxService MessageBoxService;

	public MergeCommand(MainWindowViewModel mainWindowViewModel, IMessageBoxService messageBoxService)
	{
		_mainWindowViewModel = mainWindowViewModel;
		_mainWindowViewModel.PropertyChanged += _mainWindowViewModel_PropertyChanged;
		MessageBoxService = messageBoxService;
	}

	private void _mainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MainWindowViewModel.SourceHUD) || e.PropertyName == nameof(MainWindowViewModel.TargetHUD))
		{
			OnCanExecuteChanged();
		}
	}

	public override bool CanExecute(object? parameter)
	{
		return _mainWindowViewModel.SourceHUD != null && _mainWindowViewModel.TargetHUD != null && base.CanExecute(parameter);
	}

	public override void Execute(object? parameter)
	{
		try
		{
#if !DEBUG
			static bool PathContainsPath(string parentDir, string subDir)
			{
				string relativeDirectory = Path.GetRelativePath(parentDir, subDir);
				return !relativeDirectory.StartsWith("..") && !Path.IsPathRooted(relativeDirectory);
			}

			bool teamFortress2FolderContainsTarget = PathContainsPath(Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "tf\\custom"), _mainWindowViewModel.TargetHUD!.FolderPath);

			Process[] processes;

			if (teamFortress2FolderContainsTarget)
			{
				processes = [
					..Process.GetProcessesByName("hl2"),
					..Process.GetProcessesByName("tf"),
					..Process.GetProcessesByName("tf_win64"),
				];
			}
			else
			{
				processes = [];
			}

			if (teamFortress2FolderContainsTarget && processes.Length != 0)
			{
				MessageBoxService.ShowException(new Exception("TF2 process open, cannot merge!"), "TF2 Open Error");
				return;
			}
#endif

			HUD.Merge(
				_mainWindowViewModel.SourceHUD!,
				_mainWindowViewModel.TargetHUD!,
				_mainWindowViewModel.HUDPanelViewModels
					.Where((hudPanelViewModel) => hudPanelViewModel.Selected)
					.Select((hudPanelViewModel) => hudPanelViewModel.HUDPanel)
					.ToArray()
			);

			MessageBoxService.Show("Done!");
		}
		catch (Exception e)
		{
			MessageBoxService.ShowException(e, "Error");
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			_mainWindowViewModel.PropertyChanged -= _mainWindowViewModel_PropertyChanged;
		}

		_disposed = true;
	}
}
