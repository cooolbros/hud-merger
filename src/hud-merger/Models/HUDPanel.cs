using System;
using System.Windows.Controls;

namespace HUDMerger.Models
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

		/// <summary>(Optional) Nested object that must exist inside the FilePath for this HUDFile to exist</summary>
		public KeyValueLocation RequiredKeyValue { get; init; }

		/// <summary>Dependencies to add when merging this panel</summary>
		public SchemeDependenciesManager Scheme { get; set; }

		/// <summary>(Usage) Whether the panel should be merged</summary>
		public bool Armed = false;

		// These are GUI related and should be removed/replaced if GUI changes
		public Border OriginListItem;
		public Border TargetListItem;
	}
}