using System;

namespace HUDMergerVDF.Models
{
	/// <summary>
	/// Provides options to be used with VDF.Parse
	/// </summary>
	public class VDFParseOptions
	{
		/// <summary>
		/// Allow Key/Values to span multiple lines
		/// </summary>
		public bool AllowMultilineStrings { get; init; } = false;

		/// <summary>
		/// OS Tags
		/// </summary>
		public VDFOSTags OSTags { get; init; } = VDFOSTags.All;
	}
}
