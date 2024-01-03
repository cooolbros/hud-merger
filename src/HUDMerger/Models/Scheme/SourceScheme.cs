using System;
using HUDMerger.Services;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public class SourceScheme : SchemeBase
{
	public SourceScheme() : base()
	{
	}

	public SourceScheme(IHUDFileReaderService reader, HUD hud, string relativePath) : base(reader, hud, relativePath)
	{
	}

	public SourceScheme(IHUDFileReaderService reader, HUD hud, string relativePath, KeyValues keyValues) : base(reader, hud, relativePath, keyValues)
	{
	}
}
