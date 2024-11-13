using System;
using HUDMerger.Core.Models;
using VDF.Exceptions;

namespace HUDMerger.Core.Exceptions;

public class FileException(HUD hud, string relativePath, VDFSyntaxException innerException) : Exception($"{hud.Name}: {App.PathSeparatorRegex().Replace(relativePath, "/")}: {innerException.Message}")
{
	public HUD HUD { get; } = hud;
}
