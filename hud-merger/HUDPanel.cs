using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace hud_merger
{
	/// <summary>
	/// Represents a component of the HUD
	/// </summary>
	public class HUDPanel
	{
		/// <summary>Name of HUD panel visible to user</summary>
		public string Name { get; set; }

		/// <summary>Main HUDFile set required for this HUDPanel to 'in' the HUD</summary>
		public HUDFile Main { get; set; }

		/// <summary>Other HUDFiles that contribute to this HUDPanel but are non essential</summary>
		public HUDFile[] Files { get; set; }

		/// <summary>(Usage) Whether the panel should be merged</summary>
		public bool Armed = false;

		// These are GUI related and should be removed/replaced if GUI changes
		public Label OriginListItem;
		public Label TargetListItem;
	}

	/// <summary>
	/// Represents a HUD .res file and its associated HUDLayout entry and required events
	/// </summary>
	public class HUDFile
	{
		/// <summary>Path to .res file (relative to HUD folder)</summary>
		public string FilePath { get; set; }

		/// <summary>this HUD File's associated HUDLayout entry</summary>
		public string[] HUDLayout { get; set; }

		/// <summary>Events associated with this HUD File</summary>
		/// <example>
		/// HudHealthBonusPulse, HudHealthDyingPulse
		/// </example>
		public string[] Events { get; set; }
	}

	/// <summary>
	/// Stores sets of clientscheme variable names
	/// </summary>
	/// <remarks>
	/// This class is used when adding dependencies required by HUD files when merging
	/// </remarks>
	public class ClientschemeDependencies
	{
		public HashSet<string> Colours { get; set; } = new();
		public HashSet<string> Borders { get; set; } = new();
		public HashSet<string> Fonts { get; set; } = new();
		public HashSet<string> Images { get; set; } = new();
		public HashSet<string> Audio { get; set; } = new();
	}
}