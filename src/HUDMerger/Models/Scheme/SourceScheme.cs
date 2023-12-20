using System;

namespace HUDMerger.Models.Scheme;

public class SourceScheme : SchemeBase
{
	public override string Type => "source";

	public SourceScheme()
	{
	}

	public SourceScheme(string folderPath) : base(folderPath)
	{
	}
}
