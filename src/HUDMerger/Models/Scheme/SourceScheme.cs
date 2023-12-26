using System;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public class SourceScheme : SchemeBase
{
	public SourceScheme() : base()
	{
	}

	public SourceScheme(string filePath) : base(filePath)
	{
	}

	public SourceScheme(string filePath, KeyValues keyValues) : base(filePath, keyValues)
	{
	}
}
