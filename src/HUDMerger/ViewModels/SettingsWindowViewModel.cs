using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
	private string _teamFortress2Folder;
	public string TeamFortress2Folder
	{
		get => _teamFortress2Folder;
		set
		{
			_teamFortress2Folder = value;
			OnPropertyChanged();
		}
	}

	public ICommand CancelCommand { get; }
	public ICommand ApplyCommand { get; }

	public event EventHandler? Close;

	public SettingsWindowViewModel()
	{
		_teamFortress2Folder = ((App)Application.Current).Settings.Value.TeamFortress2Folder;
		CancelCommand = new RelayCommand(Cancel);
		ApplyCommand = new RelayCommand(Apply);
	}

	private void Cancel()
	{
		Close?.Invoke(this, new EventArgs());
	}

	private void Apply()
	{
		((App)Application.Current).Settings.Value.TeamFortress2Folder = TeamFortress2Folder;

		Properties.Settings.Default.TeamFortress2Folder = TeamFortress2Folder;
		Properties.Settings.Default.Save();

		Close?.Invoke(this, new EventArgs());
	}
}
