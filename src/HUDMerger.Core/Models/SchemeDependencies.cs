using System;
using System.Collections.Generic;

namespace HUDMerger.Core.Models;

public class SchemeDependencies
{
	public HashSet<string> Colours { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> Borders { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> Fonts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	public void UnionWith(SchemeDependencies other)
	{
		Colours.UnionWith(other.Colours);
		Borders.UnionWith(other.Borders);
		Fonts.UnionWith(other.Fonts);
	}

	public bool Any()
	{
		return Colours.Count != 0 || Borders.Count != 0 || Fonts.Count != 0;
	}
}
