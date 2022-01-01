using System;

namespace HUDMerger.Models
{
	/// <summary>
	/// Represents a HUD .res file and its associated HUDLayout entry and required events
	/// </summary>
	public class HUDFile
	{
		/// <summary>Path to .res file (relative to HUD folder)</summary>
		public string FilePath { get; set; }

		/// <summary>this HUD File's associated HUDLayout entry</summary>
		public string[] HUDLayout { get; set; }

		/// <summary>
		/// Events associated with this HUD File.
		/// <example>
		/// Examples: HudHealthBonusPulse, HudHealthDyingPulse
		/// </example>
		/// </summary>
		public string[] Events { get; set; }
	}
}