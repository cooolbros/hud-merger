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

		public void Add(string HUDFile, HashSet<string> Files)
		{
			string SourceFilePath = $"{this.HUDPath}\\{HUDFile}";
			if (!File.Exists(SourceFilePath))
			{
				System.Diagnostics.Debug.WriteLine("Could not find " + SourceFilePath);
			}

			string[] Folders = HUDFile.Split('\\');
			Folders[^1] = "";
			string FolderPath = String.Join('\\', Folders);

			Dictionary<string, dynamic> Obj = File.Exists(SourceFilePath) ? Utilities.VDFTryParse(SourceFilePath) : new();
			this.Add(FolderPath, Obj, Files);
		}

		public void Add(string RelativeFolderPath, Dictionary<string, dynamic> Obj, HashSet<string> Files)
		{
			// #base
			if (Obj.ContainsKey("#base"))
			{
				List<string> BaseFiles = new();
				if (Obj["#base"].GetType() == typeof(List<dynamic>))
				{
					// List<dynamic> is not assignable to list string, add individual strings
					foreach (dynamic BaseFile in Obj["#base"])
					{
						BaseFiles.Add(BaseFile);
						// Files.Add(BaseFile)
					}
				}
				else
				{
					// Assume #base is a string
					BaseFiles.Add(Obj["#base"]);
				}

				foreach (string BaseFile in BaseFiles)
				{
					string BaseFileRelativePath = $"{RelativeFolderPath}\\{String.Join('\\', Regex.Split(BaseFile, "[\\/]+"))}";
					Files.Add(BaseFileRelativePath);
					this.Add(BaseFileRelativePath, Files);
				}
			}

			// Look at primitive properties and add matches to Dependencies Dictionary
			void IterateDictionary(Dictionary<string, dynamic> Obj)
			{
				foreach (string Key in Obj.Keys)
				{
					if (Obj[Key].GetType() == typeof(Dictionary<string, dynamic>))
					{
						IterateDictionary(Obj[Key]);
					}
					else
					{
						Type T = typeof(ClientschemeDependencies);
						foreach (PropertyInfo TypeKey in T.GetProperties())
						{
							dynamic CurrentPropertiesList = TypeKey.GetValue(Properties);
							HashSet<string> CurrentDependenciesList = (HashSet<string>)TypeKey.GetValue(this);

							foreach (string DefaultPropertyKey in CurrentPropertiesList)
							{
								if (Key.ToLower().Contains(DefaultPropertyKey.ToLower()))
								{
									// The 'subimage' property when used in resource/gamemenu.res specifies
									// an image path, other instances of the key name usually specify
									// an image element. Ignore Dictionary<string, dynamic>

									if (Obj[Key].GetType() == typeof(List<dynamic>))
									{
										foreach (dynamic DuplicateKey in Obj[Key])
										{
											if (DuplicateKey.GetType() == typeof(string))
											{
												CurrentDependenciesList.Add(DuplicateKey);
											}
										}
									}
									else if (Obj[Key].GetType() == typeof(string))
									{
										CurrentDependenciesList.Add(Obj[Key]);
									}
								}
							}
						}
					}
				}
			}

			IterateDictionary(Obj);
		}
	}
}