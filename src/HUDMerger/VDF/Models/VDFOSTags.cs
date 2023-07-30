using System;

namespace HUDMergerVDF.Models
{
	public enum VDFOSTags
	{
		/// <summary>
		/// Don't allow OS Tags
		/// </summary>
		None,

		/// <summary>
		/// Allow OS Tags on strings only
		/// </summary>
		Strings,

		/// <summary>
		/// Allow OS Tags on objects only
		/// </summary>
		Objects,

		/// <summary>
		/// Allow all OS Tags
		/// </summary>
		All
	}
}
