using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VDF.Models;

namespace VDF;

public abstract class KeyValueComparer : IEqualityComparer<KeyValue>
{
	public abstract bool Equals(KeyValue x, KeyValue y);
	public abstract int GetHashCode([DisallowNull] KeyValue obj);

	public static KeyValueComparer KeyComparer { get; } = new KeyComparer();
}

public sealed class KeyComparer : KeyValueComparer
{
	public override bool Equals(KeyValue x, KeyValue y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key) && StringComparer.OrdinalIgnoreCase.Equals(x.Conditional, y.Conditional);
	}

	public override int GetHashCode([DisallowNull] KeyValue obj)
	{
		return HashCode.Combine(obj.Key.ToLower(), obj.Conditional?.ToLower());
	}
}
