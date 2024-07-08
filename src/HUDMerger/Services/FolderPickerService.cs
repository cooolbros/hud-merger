using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using HUDMerger.Core.Services;
using Microsoft.Win32;

namespace HUDMerger.Services;

public class FolderPickerService : IFolderPickerService
{
	private readonly OpenFolderDialog OpenFolderDialog;

	public FolderPickerService(ISettingsService settingsService)
	{
		OpenFolderDialog = new OpenFolderDialog
		{
			InitialDirectory = Path.Join(settingsService.Settings.TeamFortress2Folder, "tf\\custom\\")
		};
	}

	public async Task<string?> PickFolderAsync()
	{
		return await Task.Run(() =>
			OpenFolderDialog.ShowDialog(Application.Current.MainWindow) == true
				? OpenFolderDialog.FolderName
				: null
		);
	}
}
