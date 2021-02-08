using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace hud_merger
{
	public class HUDPanel
	{
		public string Name { get; set; }
		public HUDFile Main { get; set; }
		public HUDFile[] Files { get; set; }
		public bool Armed = false;
		public Label OriginListItem;
		public Label TargetListItem;
	}

	public class HUDFile
	{
		public string FilePath { get; set; }
		public string HUDLayout { get; set; }
	}

	public class Dependencies
	{
		public List<string> Colours;
		public List<string> Borders;
		public List<string> Fonts;
		public List<string> HUDLayoutEntries = new List<string>();
		public List<string> BinaryFiles = new List<string>();
	}

}