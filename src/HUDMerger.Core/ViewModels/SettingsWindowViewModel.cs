using System;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using HUDMerger.Core.Models;
using HUDMerger.Core.Services;

namespace HUDMerger.Core.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
	private readonly ISettingsService SettingsService;

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

	public SettingsWindowViewModel(ISettingsService settingsService)
	{
		SettingsService = settingsService;

		_teamFortress2Folder = SettingsService.Settings.TeamFortress2Folder;
		_language = SettingsService.Settings.Language;

		CancelCommand = new RelayCommand(Cancel);
		ApplyCommand = new RelayCommand(Apply);
	}

	private void Cancel()
	{
		Close?.Invoke(this, new EventArgs());
	}

	private void Apply()
	{
		SettingsService.Settings.TeamFortress2Folder = TeamFortress2Folder;
		SettingsService.Settings.Language = Language;
		SettingsService.Save();

		Close?.Invoke(this, new EventArgs());
	}
}
