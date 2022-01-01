using System;

namespace HUDMerger.Models
{
	/// <summary>
	/// Stores a file and key path to a Key/Value
	/// </summary>
	public class KeyValueLocation
	{
		/// <summary>
		/// Relative path to .res file
		/// </summary>
		public string FilePath { get; init; }

		/// <summary>
		/// Path to Key/Value seperated by '.'
		/// </summary>
		public string KeyPath { get; init; }
	}
}