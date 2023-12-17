using System;

namespace HUDMerger.Models;

/// <summary>
/// Stores a file and key path to a Key/Value
/// </summary>
public class KeyValueLocation
{
	/// <summary>
	/// Relative path to .res file
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Path to Key/Value
	/// </summary>
	public required string[] KeyPath { get; init; }
}
