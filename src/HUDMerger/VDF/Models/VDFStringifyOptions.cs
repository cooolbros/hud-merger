using System;

namespace HUDMergerVDF.Models;

public class VDFStringifyOptions
{
	/// <summary>
	/// VDF Stringify indentation character
	/// </summary>
	public VDFIndentation Indentation { get; init; } = VDFIndentation.Tabs;

	/// <summary>
	/// VDF Stringify tab size
	/// </summary>
	public int TabSize { get; init; } = 4;

	/// <summary>
	/// VDF Stringify line endings
	/// </summary>
	public VDFNewLine NewLine { get; init; } = VDFNewLine.CRLF;
}
