using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
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
		public Border OriginListItem;
		public Border TargetListItem;
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

		/// <summary>
		/// Events associated with this HUD File.
		/// <example>
		/// Examples: HudHealthBonusPulse, HudHealthDyingPulse
		/// </example>
		/// </summary>
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
		private static ClientschemeDependencies Properties = JsonSerializer.Deserialize<ClientschemeDependencies>(File.ReadAllText("Resources\\Clientscheme.json"));
		public string HUDPath;
		public HashSet<string> Colours { get; set; } = new();
		public HashSet<string> Borders { get; set; } = new();
		public HashSet<string> Fonts { get; set; } = new();
		public HashSet<string> Images { get; set; } = new();
		public HashSet<string> Audio { get; set; } = new();

		public void Add(string hudFile, HashSet<string> files)
		{
			string sourceFilePath = $"{this.HUDPath}\\{hudFile}";
			if (!File.Exists(sourceFilePath))
			{
				System.Diagnostics.Debug.WriteLine("Could not find " + sourceFilePath);
			}

			string[] folders = hudFile.Split('\\');
			folders[^1] = "";
			string folderPath = String.Join('\\', folders);

			Dictionary<string, dynamic> obj = File.Exists(sourceFilePath) ? Utilities.VDFTryParse(sourceFilePath) : new();
			this.Add(folderPath, obj, files);
		}

		public void Add(string relativeFolderPath, Dictionary<string, dynamic> obj, HashSet<string> files)
		{
			// #base
			if (obj.ContainsKey("#base"))
			{
				List<string> baseFiles = new();
				if (obj["#base"].GetType() == typeof(List<dynamic>))
				{
					// List<dynamic> is not assignable to list string, add individual strings
					foreach (dynamic baseFile in obj["#base"])
					{
						baseFiles.Add(baseFile);
						// Files.Add(BaseFile)
					}
				}
				else
				{
					// Assume #base is a string
					baseFiles.Add(obj["#base"]);
				}

				foreach (string baseFile in baseFiles)
				{
					string baseFileRelativePath = $"{relativeFolderPath}\\{String.Join('\\', Regex.Split(baseFile, "[\\/]+"))}";
					files.Add(baseFileRelativePath);
					this.Add(baseFileRelativePath, files);
				}
			}

			// Look at primitive properties and add matches to Dependencies Dictionary
			void IterateDictionary(Dictionary<string, dynamic> obj)
			{
				foreach (string key in obj.Keys)
				{
					if (obj[key].GetType() == typeof(Dictionary<string, dynamic>))
					{
						IterateDictionary(obj[key]);
					}
					else
					{
						Type T = typeof(ClientschemeDependencies);
						foreach (PropertyInfo typeKey in T.GetProperties())
						{
							dynamic currentPropertiesList = typeKey.GetValue(Properties);
							HashSet<string> currentDependenciesList = (HashSet<string>)typeKey.GetValue(this);

							foreach (string defaultPropertyKey in currentPropertiesList)
							{
								if (key.ToLower().Contains(defaultPropertyKey.ToLower()))
								{
									// The 'subimage' property when used in resource/gamemenu.res specifies
									// an image path, other instances of the key name usually specify
									// an image element. Ignore Dictionary<string, dynamic>

									if (obj[key].GetType() == typeof(List<dynamic>))
									{
										foreach (dynamic duplicateKey in obj[key])
										{
											if (duplicateKey.GetType() == typeof(string))
											{
												currentDependenciesList.Add(duplicateKey);
											}
										}
									}
									else if (obj[key].GetType() == typeof(string))
									{
										currentDependenciesList.Add(obj[key]);
									}
								}
							}
						}
					}
				}
			}

			IterateDictionary(obj);
		}
	}
}