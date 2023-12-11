using System;
using System.Linq;
using System.Collections.Generic;

namespace VDF.Models;

public class KeyValues : List<KeyValue>
{
	public KeyValues() { }
	public KeyValues(IEnumerable<KeyValue> collection) : base(collection) { }

	public HashSet<KeyValue> ToHashSet()
	{
		return this
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
