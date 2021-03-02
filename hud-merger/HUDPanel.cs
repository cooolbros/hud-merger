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
		public string[] HUDLayout { get; set; }
		public string[] Events { get; set; }
	}

	public class ClientschemeDependencies
	{
		public HashSet<string> Colours { get; set; } = new();
		public HashSet<string> Borders { get; set; } = new();
		public HashSet<string> Fonts { get; set; } = new();
		public HashSet<string> Images { get; set; } = new();
		public HashSet<string> Audio { get; set; } = new();
	}
}