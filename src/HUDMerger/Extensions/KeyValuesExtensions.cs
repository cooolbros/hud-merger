using System;
using System.Collections.Generic;
using System.Linq;
using VDF;
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

	public static void ForAll(this KeyValues source, Action<KeyValue> action)
	{
		foreach (KeyValue keyValue in source)
		{
			if (keyValue.Value is KeyValues keyValues)
			{
				keyValues.ForAll(action);
			}
			action(keyValue);
		}
	}

	public static HashSet<KeyValue> ToHashSet(this KeyValues source)
	{
		return source
			.Select((kv) => new KeyValue
			{
				Key = kv.Key,
				Value = kv.Value switch
				{
					string str => str,
					KeyValues keyValues => keyValues.ToHashSet(),
					_ => throw new NotSupportedException()
				},
				Conditional = kv.Conditional
			})
			.ToHashSet(KeyValueComparer.KeyComparer);
	}
}
