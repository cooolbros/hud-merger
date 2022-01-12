using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HUDMerger.Models
{
	public class FilesHashSet : HashSet<string>
	{
		public FilesHashSet() : base(new FilePathComparer())
		{
		}

		public new bool Add(string item)
		{
			return base.Add(FilesHashSet.EncodeFilePath(item));
		}

		public static string EncodeFilePath(string filePath)
		{
			return String.Join('\\', Regex.Split(filePath, @"[/\\]+"));
		}
	}

	class FilePathComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			return x == FilesHashSet.EncodeFilePath(y);
		}

		public int GetHashCode(string x)
		{
			return FilesHashSet.EncodeFilePath(x).GetHashCode();
		}
	}
}
