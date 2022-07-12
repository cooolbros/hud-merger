using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace HUDMerger.Models
{
	public class FilesHashSet : HashSet<string>
	{
		private class FilePathComparer : IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				return x == Encode(y);
			}

			public int GetHashCode([DisallowNull] string obj)
			{
				return Encode(obj).GetHashCode();
			}

			public static string Encode(string filePath)
			{
				return String.Join('\\', Regex.Split(filePath, @"[/\\]+"));
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
}
