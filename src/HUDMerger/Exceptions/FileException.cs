using System;
using HUDMerger.Models;
using VDF.Exceptions;

namespace HUDMerger.Exceptions;

public class FileException(HUD hud, string relativePath, VDFSyntaxException innerException) : Exception($"{hud.Name} - {relativePath}: {innerException.Message}")
{
}
