using System;
using HUDMerger.Core.Models;

namespace HUDMerger.Core.Services;

public interface ISettingsService
{
	public Settings Settings { get; }
	public void Save();
}
