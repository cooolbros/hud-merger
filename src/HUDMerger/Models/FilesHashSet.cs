using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HUDMerger.Models;

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
}
