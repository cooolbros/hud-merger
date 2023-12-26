using System;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public class ClientScheme : SchemeBase
{
	public ClientScheme() : base()
	{
	}

	public ClientScheme(string filePath) : base(filePath)
	{
	}

	public ClientScheme(string filePath, KeyValues keyValues) : base(filePath, keyValues)
	{
	}
}
