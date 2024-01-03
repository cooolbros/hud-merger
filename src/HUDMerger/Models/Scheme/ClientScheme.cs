using System;
using HUDMerger.Services;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public class ClientScheme : SchemeBase
{
	public ClientScheme() : base()
	{
	}

	public ClientScheme(IHUDFileReaderService reader, HUD hud, string relativePath) : base(reader, hud, relativePath)
	{
	}

	public ClientScheme(IHUDFileReaderService reader, HUD hud, string relativePath, KeyValues keyValues) : base(reader, hud, relativePath, keyValues)
	{
	}
}
