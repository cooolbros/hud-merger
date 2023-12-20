using System;
using System.Collections.Generic;

namespace HUDMerger.Models;

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
}
