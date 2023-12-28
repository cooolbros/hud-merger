using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMerger.Extensions;
using VDF;
using VDF.Models;

namespace HUDMerger.Models;

public class HUDLayout
{
	private readonly Dictionary<KeyValue, HashSet<KeyValue>> Entries = new(KeyValueComparer.KeyComparer);

	public HUDLayout()
	{
	}

	public HUDLayout(string folderPath)
	{
		static Dictionary<KeyValue, HashSet<KeyValue>>? ReadBaseFile(FileInfo file)
		{
			if (!file.Exists) return null;

			KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(file.FullName));
			Dictionary<KeyValue, HashSet<KeyValue>> entries = new(KeyValueComparer.KeyComparer);

			foreach (KeyValue entry in keyValues.Header(strict: true))
			{
				if (entry.Value is KeyValues values)
				{
					entries.TryAdd(entry, new(KeyValueComparer.KeyComparer));
					entries[entry].UnionWithRecursive(values);
				}
			}

			foreach (string baseFile in keyValues.BaseFiles())
			{
				foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> entry in ReadBaseFile(new FileInfo(Path.Join(file.DirectoryName, baseFile))) ?? [])
				{
					entries.TryAdd(entry.Key, new(KeyValueComparer.KeyComparer));
					entries[entry.Key].UnionWithRecursive(entry.Value);
				}
			}

			return entries;
		}

		string hudLayoutPath = Path.Join(folderPath, "scripts\\hudlayout.res");

		KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(File.Exists(hudLayoutPath) ? hudLayoutPath : "Resources\\HUD\\scripts\\hudlayout.res"));

		Dictionary<KeyValue, KeyValues> entries = new(KeyValueComparer.KeyComparer);

		foreach (KeyValue entry in keyValues.Header(strict: true))
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
			foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> entry in ReadBaseFile(new FileInfo(Path.Join(Path.GetDirectoryName(hudLayoutPath), baseFile))) ?? [])
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
