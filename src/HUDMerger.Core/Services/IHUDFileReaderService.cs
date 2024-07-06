using System;
using System.Collections.Generic;
using HUDAnimations.Models;
using HUDMerger.Models;
using VDF.Models;

namespace HUDMerger.Core.Services;

public interface IHUDFileReaderService
{
	public void Require(IEnumerable<(HUD hud, string relativePath, FileType type)> filePaths);

	public KeyValues ReadKeyValues(HUD hud, string relativePath);
	public KeyValues? TryReadKeyValues(HUD hud, string relativePath);

	public HUDAnimationsFile ReadHUDAnimations(HUD hud, string relativePath);
	public HUDAnimationsFile? TryReadHUDAnimations(HUD hud, string relativePath);
}
