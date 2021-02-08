using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace hud_merger
{
	public class HUD
	{
		public string Name;
		public string FolderPath;

		public HUD(string FolderPath)
		{
			string[] Folders = FolderPath.Split("\\");
			this.Name = Folders[Folders.Length - 1];
			this.FolderPath = FolderPath;
		}

		public bool TestPanel(HUDPanel Panel)
		{
			return File.Exists(this.FolderPath + "\\" + Panel.Main.FilePath);
		}
	}
}