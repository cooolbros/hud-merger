using System;

namespace HUDMerger.Models.Scheme;

public class ClientScheme : SchemeBase
{
	public override string Type => "client";

	public ClientScheme()
	{
	}

	public ClientScheme(string folderPath) : base(folderPath)
	{
	}
}
