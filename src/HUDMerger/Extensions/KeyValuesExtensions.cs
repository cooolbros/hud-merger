using System;
using System.Collections.Generic;
using System.Linq;
using VDF.Models;

namespace HUDMerger.Extensions;

public static class KeyValuesExtensions
{
	public static IEnumerable<string> BaseFiles(this KeyValues source)
	{
		return source
			.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "#base"))
			.Select((kv) => kv.Value)
			.OfType<string>();
	}

	public static KeyValues Header(this KeyValues source, bool strict = false)
	{
		// File header does not respect conditional
		dynamic? value = source.FirstOrDefault((kv) => !StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "#base")).Value;

		return value switch
		{
			KeyValues header => header,
			string str when strict => throw new NotSupportedException(),
			_ => [],
		};
	}
}
