using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HUDMerger.Core.Models;

public class FilesHashSet : HashSet<string>
{
	private class FilePathComparer : IEqualityComparer<string>
	{
		public bool Equals(string? x, string? y)
		{
			return x == (y != null ? Encode(y) : null);
		}

		public int GetHashCode([DisallowNull] string obj)
		{
			return Encode(obj).GetHashCode();
		}

		public static string Encode(string filePath)
		{
			return App.PathSeparatorRegex().Replace(filePath, "\\");
		}
	}

	public FilesHashSet() : base(new FilePathComparer())
	{
	}

	public new bool Add(string item)
	{
		return base.Add(FilePathComparer.Encode(item));
	}

	public new void UnionWith(IEnumerable<string> other)
	{
		base.UnionWith(other.Select(FilePathComparer.Encode));
	}
}
