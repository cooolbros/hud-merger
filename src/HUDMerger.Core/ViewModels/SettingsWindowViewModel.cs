using System;
using System.Windows;
using System.Windows.Input;
using HUDMerger.Models;
using Microsoft.Toolkit.Mvvm.Input;

namespace HUDMerger.Core.ViewModels;

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

	private string _language;
	public string Language
	{
		get => _language;
		set
		{
			_language = value;
			OnPropertyChanged();
		}
	}

	public ICommand CancelCommand { get; }
	public ICommand ApplyCommand { get; }

	public event EventHandler? Close;

	public SettingsWindowViewModel()
	{
		Settings settings = ((App)Application.Current).Settings.Value;

		_teamFortress2Folder = settings.TeamFortress2Folder;
		_language = settings.Language;

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
		((App)Application.Current).Settings.Value.Language = Language;

		Properties.Settings.Default.TeamFortress2Folder = TeamFortress2Folder;
		Properties.Settings.Default.Language = Language;
		Properties.Settings.Default.Save();

		Close?.Invoke(this, new EventArgs());
	}
}
