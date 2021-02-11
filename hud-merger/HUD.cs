using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public void Merge(HUD Origin, HUDPanel[] Panels)
		{
			// How to merge:
			// Create list of files that includes Main.FilePath and all Files[i].FilePath
			// For each file, read lines and add #base paths to file list
			// For each file, create a set of borders, colours, fonts, images and sound files
			// Parse clientscheme.res (and #base files) and store dependencies in a Dictionary
			// Create a set of used font Names of the font definitions used
			// Find custom font definition that matches the font names and add to Clientscheme Dictionary
			// Add font files to file list
			// Copy all files


			#region Create list of all files to evaluate for merge
			List<string> Files = new();
			List<string> HUDLayoutEntries = new();

			ClientschemeDependencies Properties = JsonSerializer.Deserialize<ClientschemeDependencies>(File.ReadAllText("Resources\\Clientscheme.json"));

			foreach (HUDPanel Panel in Panels)
			{
				Files.Add(Panel.Main.FilePath);
				if (Panel.Main.HUDLayout != null)
				{
					foreach (string HUDLayoutEntry in Panel.Main.HUDLayout)
					{
						HUDLayoutEntries.Add(HUDLayoutEntry);
					}
				}
				if (Panel.Files != null)
				{
					foreach (HUDFile HUDFile in Panel.Files)
					{
						Files.Add(HUDFile.FilePath);
					}
				}
			}

			#endregion

			#region Iterate all files and #base files recursively, then add clientscheme related properties to Dependencies set
			ClientschemeDependencies Dependencies = new();
			void AddDependencies(string FilePath)
			{
				Dictionary<string, dynamic> Obj = VDF.Parse(File.ReadAllText(FilePath));

				// #base
				if (Obj.ContainsKey("#base"))
				{
					List<string> BaseFiles = new();
					if (Obj["#base"].GetType().Name.Contains("List"))
					{
						BaseFiles = Obj["#base"];
					}
					else
					{
						// Assume #base is a string
						BaseFiles.Add(Obj["#base"]);
					}

					string[] Folders = FilePath.Split("\\");
					// Remove File Name
					Folders[Folders.Length - 1] = "";
					foreach (string BaseFile in BaseFiles)
					{
						AddDependencies(String.Join('\\', Folders) + BaseFile);
					}
				}

				// Look at primitive properties and add matches to Dependencies Dictionary
				void IterateDictionary(Dictionary<string, dynamic> Obj)
				{
					foreach (string Key in Obj.Keys)
					{
						if (Obj[Key].GetType().Name.Contains("Dictionary"))
						{
							IterateDictionary(Obj[Key]);
						}
						else
						{
							// Colours
							foreach (string DefaultPropertyKey in Properties.Colours)
							{
								if (Key == DefaultPropertyKey)
								{
									Dependencies.Colours.Add(Obj[Key]);
								}
							}

							// Borders
							foreach (string DefaultPropertyKey in Properties.Borders)
							{
								if (Key == DefaultPropertyKey)
								{
									Dependencies.Borders.Add(Obj[Key]);
								}
							}

							// Fonts
							foreach (string DefaultPropertyKey in Properties.Fonts)
							{
								if (Key == DefaultPropertyKey)
								{
									Dependencies.Fonts.Add(Obj[Key]);
								}
							}

							// Images
							foreach (string DefaultPropertyKey in Properties.Images)
							{
								if (Key == DefaultPropertyKey)
								{
									Dependencies.Images.Add(Obj[Key]);
								}
							}

							// Audio
							foreach (string DefaultPropertyKey in Properties.Audio)
							{
								if (Key == DefaultPropertyKey)
								{
									Dependencies.Audio.Add(Obj[Key]);
								}
							}
						}
					}
				}

				IterateDictionary(Obj);
			}

			// Evaluate files requested for merge
			foreach (string HUDFile in Files)
			{
				string SourceFileName = Origin.FolderPath + "\\" + HUDFile;
				if (File.Exists(SourceFileName))
				{
					AddDependencies(SourceFileName);
				}
			}

			#endregion

			#region Load origin HUD Clientscheme

			string OriginClientschemePath = Origin.FolderPath + "\\resource\\clientscheme.res";
			Dictionary<string, dynamic> OriginClientscheme = Utilities.LoadControls(OriginClientschemePath);

			#endregion

			#region Add all required properties in Dependencies from OriginClientscheme to NewClientscheme

			Dictionary<string, dynamic> NewClientscheme = new();
			NewClientscheme.Add("Colors", new Dictionary<string, dynamic>());
			NewClientscheme.Add("Borders", new Dictionary<string, dynamic>());
			NewClientscheme.Add("Fonts", new Dictionary<string, dynamic>());
			NewClientscheme.Add("CustomFontFiles", new Dictionary<string, dynamic>());

			// Colours
			foreach (string ColourProperty in Dependencies.Colours)
			{
				if (OriginClientscheme?["Scheme"]?["Colors"].ContainsKey(ColourProperty))
				{
					NewClientscheme["Colors"].Add(ColourProperty, OriginClientscheme["Scheme"]["Colors"][ColourProperty]);
				}
			}

			// Borders
			foreach (string BorderProperty in Dependencies.Borders)
			{
				if (OriginClientscheme?["Scheme"]?["Borders"].ContainsKey(BorderProperty))
				{
					NewClientscheme["Borders"].Add(BorderProperty, OriginClientscheme["Scheme"]["Borders"][BorderProperty]);
				}
			}

			// Fonts
			HashSet<string> FontNames = new();
			foreach (string FontProperty in Dependencies.Fonts)
			{
				if (OriginClientscheme?["Scheme"]?["Fonts"].ContainsKey(FontProperty))
				{
					dynamic FontDefinition = OriginClientscheme["Scheme"]["Fonts"][FontProperty];
					NewClientscheme["Fonts"].Add(FontProperty, FontDefinition);
					foreach (dynamic FontDefinitionNumber in FontDefinition.Keys)
					{
						FontNames.Add(FontDefinition?[FontDefinitionNumber]?["name"]);
					}
				}
			}

			// Add custom fonts
			Dictionary<string, dynamic> OriginalCustomFontFiles = OriginClientscheme?["Scheme"]?["CustomFontFiles"];
			foreach (string CustomFontFileNumber in OriginalCustomFontFiles.Keys)
			{
				foreach (string FontName in FontNames)
				{
					// CustomFontFiles 1 & 2 are just strings
					// "1" "resource/tf.ttf"
					// "2" "resource/tfd.ttf"
					if (OriginalCustomFontFiles?[CustomFontFileNumber].GetType().Name.Contains("Dictionary"))
					{
						if (FontName == OriginalCustomFontFiles?[CustomFontFileNumber]?["name"])
						{
							NewClientscheme["CustomFontFiles"].Add(CustomFontFileNumber, OriginalCustomFontFiles?[CustomFontFileNumber]);

							// Add .ttf file as well
							Files.Add(OriginalCustomFontFiles?[CustomFontFileNumber]?["font"]);
						}
					}
				}
			}

			#endregion

			#region Add all required HUD Layout entries from OriginHUDLayout to NewHUDLayout

			string OriginHUDLayoutPath = Origin.FolderPath + "\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> OriginHUDLayout = Utilities.LoadControls(OriginHUDLayoutPath);

			string ThisHUDLayoutPath = $"{this.FolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> NewHUDLayout = Utilities.LoadControls(File.Exists(ThisHUDLayoutPath) ? ThisHUDLayoutPath : "Resources\\hudlayout.res");
			// Dont Utilities.Merge because the default hudlayout contains os tags for $WIN32 which will override regular xpos ypos properties

			// it is common for custom huds to remove os tags so override entries
			foreach (string HUDLayoutEntry in HUDLayoutEntries)
			{
				if (OriginHUDLayout?["Resource/HudLayout.res"].ContainsKey(HUDLayoutEntry))
				{
					NewHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry] = OriginHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry];
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"{Origin.Name}'s hudlayout does not contain {HUDLayoutEntry}!");
				}
			}

			#endregion

			#region Write files

			foreach (string ImagePath in Dependencies.Images)
			{
				string[] Folders = System.Text.RegularExpressions.Regex.Split(ImagePath, "[\\\\/]+");
				Files.Add($"{Origin.FolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vmt");
				Files.Add($"{Origin.FolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vtf");
			}

			foreach (string AudioPath in Dependencies.Audio)
			{
				string[] Folders = System.Text.RegularExpressions.Regex.Split(AudioPath, "[\\\\/]+");
				Files.Add($"{Origin.FolderPath}\\sound\\{String.Join("\\", Folders)}");
			}

			foreach (string FilePath in Files)
			{
				string SourceFileName = $"{Origin.FolderPath}\\{FilePath}";
				if (File.Exists(SourceFileName))
				{
					string DestFileName = $"{this.FolderPath}\\{FilePath}";
					Directory.CreateDirectory(Path.GetDirectoryName(DestFileName));
					File.Copy(SourceFileName, DestFileName, true);
				}
			}

			// Clientscheme
			bool WriteBaseStatement = true;
			if (File.Exists($"{this.FolderPath}\\resource\\clientscheme.res"))
			{
				// 'this' HUD already has a hudlayout
				foreach (string Line in File.ReadAllLines($"{this.FolderPath}\\resource\\clientscheme.res"))
				{
					if (Line.Contains($"clientscheme_{Origin.Name}.res"))
					{
						WriteBaseStatement = false;
						break;
					}
				}
			}
			else
			{
				// If clientscheme doesn't exist it is crucial to have one with default tf properties
				File.Copy("Resources/clientscheme.res", $"{this.FolderPath}\\resource\\clientscheme.res");
			}

			if (WriteBaseStatement)
			{
				File.AppendAllLines($"{this.FolderPath}\\resource\\clientscheme.res", new string[]
				{
				$"\"#base\" \"clientscheme_{Origin.Name}.res\""
				});
			}

			// ALWAYS create new clientscheme.res
			Dictionary<string, dynamic> NewClientschemeContainer = new();
			NewClientschemeContainer["Scheme"] = NewClientscheme;
			Directory.CreateDirectory($"{this.FolderPath}\\resource");
			File.WriteAllText($"{this.FolderPath}\\resource\\clientscheme_{Origin.Name}.res", VDF.Stringify(NewClientschemeContainer));

			// HUD Layout
			Directory.CreateDirectory($"{this.FolderPath}\\scripts");
			File.WriteAllText($"{this.FolderPath}\\scripts\\hudlayout.res", VDF.Stringify(NewHUDLayout));

			// UI Version
			if (!File.Exists($"{this.FolderPath}\\info.vdf"))
			{
				Dictionary<string, dynamic> InfoVDF = new();
				InfoVDF[this.Name] = new Dictionary<string, dynamic>();
				InfoVDF[this.Name]["ui_version"] = 3;
				File.WriteAllText($"{this.FolderPath}\\info.vdf", VDF.Stringify(InfoVDF));
			}

			#endregion
		}
	}
}