using System;
using System.Threading.Tasks;

namespace HUDMerger.Core.Services;

public interface IFolderPickerService
{
	public Task<string?> PickFolderAsync();
}
