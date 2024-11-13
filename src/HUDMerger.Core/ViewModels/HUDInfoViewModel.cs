using System;
using HUDMerger.Core.Models;

namespace HUDMerger.Core.ViewModels;

public class HUDInfoViewModel(string locationType, HUD? hud) : ViewModelBase
{
	private readonly string _locationType = locationType;

	private HUD? _hud = hud;
	public HUD? HUD
	{
		get => _hud;
		set
		{
			_hud = value;
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(FolderPath));
		}
	}

	public string? Name => _hud?.Name.Replace("_", "__");
	public string? FolderPath => _hud?.FolderPath ?? $"HUD to copy files {_locationType}";
}
