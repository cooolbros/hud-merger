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

	public static KeyValues Header(this KeyValues source, bool strict, string? name = null)
	{
		// File header does not respect conditional
		dynamic? value = source.FirstOrDefault((kv) => !StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "#base")).Value;

		switch (value)
		{
			case KeyValues existing:
				return existing;
			case string str when strict:
				// case when TF2 would not start if file contained string header (hudlayout.res)
				throw new NotSupportedException();
			case null when name is not null:
				// If the default header name is provided, create the header and append it to the root node
				KeyValue header = new() { Key = name, Value = new KeyValues(), Conditional = null };
				source.Add(header);
				return header.Value;
			default:
				// Entry file only contains #base files or is empty
				// If name is not provided the value cannot be set in the root
				return [];
		}
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

	public static HashSet<KeyValue> ToHashSetRecursive(this KeyValues source)
	{
		return source
			.Select((kv) => new KeyValue
			{
				Key = kv.Key,
				Value = kv.Value switch
				{
					string str => str,
					KeyValues keyValues => keyValues.ToHashSetRecursive(),
					_ => throw new NotSupportedException()
				},
				Conditional = kv.Conditional
			})
			.ToHashSet(KeyValueComparer.KeyComparer);
	}
}
