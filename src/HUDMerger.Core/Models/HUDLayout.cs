using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMerger.Extensions;
using HUDMerger.Services;
using VDF;
using VDF.Models;

namespace HUDMerger.Core.Models;

public class HUDLayout
{
	private readonly Dictionary<KeyValue, HashSet<KeyValue>> Entries = new(KeyValueComparer.KeyComparer);

	public HUDLayout()
	{
	}

	public HUDLayout(IHUDFileReaderService reader, HUD hud)
	{
		static Dictionary<KeyValue, HashSet<KeyValue>>? ReadBaseFile(IHUDFileReaderService reader, HUD hud, string relativePath)
		{
			KeyValues? keyValues = reader.TryReadKeyValues(hud, relativePath);
			if (keyValues == null) return null;

			Dictionary<KeyValue, HashSet<KeyValue>> entries = new(KeyValueComparer.KeyComparer);

			foreach (KeyValue entry in keyValues.Header())
			{
				if (entry.Value is KeyValues values)
				{
					entries.TryAdd(entry, new(KeyValueComparer.KeyComparer));
					entries[entry].UnionWithRecursive(values);
				}
			}

			foreach (string baseFile in keyValues.BaseFiles())
			{
				foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> entry in ReadBaseFile(reader, hud, Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(relativePath), baseFile))) ?? [])
				{
					entries.TryAdd(entry.Key, new(KeyValueComparer.KeyComparer));
					entries[entry.Key].UnionWithRecursive(entry.Value);
				}
			}

			return entries;
		}

		KeyValues keyValues = reader.ReadKeyValues(hud, "scripts\\hudlayout.res");

		Dictionary<KeyValue, KeyValues> entries = new(KeyValueComparer.KeyComparer);

		foreach (KeyValue entry in keyValues.Header())
		{
			if (entry.Value is KeyValues values)
			{
				entries.TryAdd(entry, []);
				foreach (KeyValue keyValue in new HashSet<KeyValue>(values, KeyValueComparer.KeyComparer))
				{
					int index = entries[entry].FindIndex((kv) => KeyValueComparer.KeyComparer.Equals(keyValue, kv));
					if (index != -1)
					{
						KeyValue kv = entries[entry][index];
						entries[entry][index] = new KeyValue
						{
							Key = kv.Key,
							Value = keyValue.Value,
							Conditional = kv.Conditional
						};
					}
					else
					{
						entries[entry].Add(keyValue);
					}
				}
			}
		}

		Entries = entries.ToDictionary(
			(kv) => kv.Key,
			(kv) => kv.Value.ToHashSetRecursive(),
			KeyValueComparer.KeyComparer
		);

		foreach (string baseFile in keyValues.BaseFiles())
		{
			foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> entry in ReadBaseFile(reader, hud, Path.GetRelativePath(".", $"scripts\\{baseFile}")) ?? [])
			{
				Entries.TryAdd(entry.Key, new(KeyValueComparer.KeyComparer));
				Entries[entry.Key].UnionWithRecursive(entry.Value);
			}
		}
	}

	public Dictionary<KeyValue, HashSet<KeyValue>> this[string index] => Entries
		.Where((entry) => StringComparer.OrdinalIgnoreCase.Equals(entry.Key.Key, index))
		.ToDictionary();
}
