using System;
using System.IO;
using System.Windows.Controls;

namespace HUDMerger.Models
{
	public class HUDBackup
	{
		private readonly FileInfo _fileInfo;
		public string HUDName { get; }
		public string Name => _fileInfo.Name;
		public string FullName => _fileInfo.FullName;
		public DateTime CreationTime => _fileInfo.CreationTime;

		public HUDBackup(string hudName, FileInfo fileInfo)
		{
			HUDName = hudName;
			_fileInfo = fileInfo;
		}
	}
}
