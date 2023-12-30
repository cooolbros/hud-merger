using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace VDF.Models;

public class KeyValues : List<KeyValue>
{
	public KeyValues() { }
	public KeyValues(IEnumerable<KeyValue> collection) : base(collection) { }

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}

		return this.SequenceEqual((KeyValues)obj);
	}

	public bool Equals([NotNullWhen(true)] object? obj, KeyValueComparer comparer)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}

		return this.SequenceEqual((KeyValues)obj, comparer);
	}

	public override int GetHashCode()
	{
		HashCode hash = new();
		foreach (KeyValue kv in this)
		{
			hash.Add(kv);
		}

		return hash.ToHashCode();
	}
}
