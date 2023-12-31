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
			.Select((kv) => App.PathSeparatorRegex().Replace(kv.Value, "\\"))
			.OfType<string>();
	}

	public static KeyValues Header(this KeyValues source, string? name = null)
	{
		// File header does not respect conditional
		KeyValue? keyValue = source.FirstOrDefault((kv) => !StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "#base"));

		switch (keyValue.Value.Value)
		{
			case KeyValues existing:
				return existing;
			case string str:
				// TF2 fails the file parse if file contained string header (hudlayout.res or hudanimations_manifest.txt)
				// Since header should not be a string just replace it with a new one
				KeyValues keyValues = [];
				source[source.IndexOf(keyValue.Value)] = new KeyValue
				{
					Key = keyValue.Value.Key,
					Value = keyValues,
					Conditional = keyValue.Value.Conditional // Preserve conditional even though it's ignored
				};
				return keyValues;
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

	public static void ForAll(this IEnumerable<KeyValue> source, Action<KeyValue> action)
	{
		foreach (KeyValue keyValue in source)
		{
			if (keyValue.Value is IEnumerable<KeyValue> keyValues)
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
