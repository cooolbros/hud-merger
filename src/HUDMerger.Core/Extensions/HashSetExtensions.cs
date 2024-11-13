using System;
using System.Collections.Generic;
using VDF.Models;

namespace HUDMerger.Core.Extensions;

public static class HashSetExtensions
{
	public static void UnionWithRecursive(this HashSet<KeyValue> source, IEnumerable<KeyValue> other)
	{
		foreach (KeyValue kv in other)
		{
			if (source.TryGetValue(kv, out KeyValue actualValue))
			{
				if (actualValue.Value is HashSet<KeyValue> existing && kv.Value is IEnumerable<KeyValue> keyValues)
				{
					existing.UnionWithRecursive(keyValues);
				}
			}
			else
			{
				source.Add(new KeyValue
				{
					Key = kv.Key,
					Value = kv.Value switch
					{
						string str => str,
						KeyValues values => values.ToHashSetRecursive(),
						HashSet<KeyValue> set => set, // Assume set was created with KeyValues.ToHashSetRecursive()
						_ => throw new NotSupportedException(),
					},
					Conditional = kv.Conditional,
				});
			}
		}
	}
}
