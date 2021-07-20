using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace hud_merger
{
	/// <summary>
	/// Represents a custom HUD
	/// </summary>
	public class HUD
	{
		/// <summary>HUD Name (name of HUD folder)</summary>
		public string Name;

		/// <summary>Absolute path to HUD Folder</summary>
		public string FolderPath;

		public HUD(string FolderPath)
		{
			this.Name = FolderPath.Split('\\')[^1];
			this.FolderPath = FolderPath;
		}

		/// <summary>Returns whether the provided HUDPanel is 'in' this HUD</summary>
		public bool TestPanel(HUDPanel Panel)
		{
			return File.Exists($"{this.FolderPath}\\{Panel.Main.FilePath}");
		}

		/// <summary>Merges an array of HUDPanels from another HUD into this HUD</summary>
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

			(HashSet<string> Files, HashSet<string> HUDLayoutEntries, HashSet<string> Events) = HUD.DestructurePanels(Panels);
			ClientschemeDependencies Dependencies = this.GetDependencies(Origin.FolderPath, Files);
			this.WriteHUDLayout(Origin.FolderPath, HUDLayoutEntries, Dependencies, Files);
			this.WriteHUDAnimations(Origin.FolderPath, Events, Origin.Name, Dependencies, Files);
			Dictionary<string, dynamic> NewClientscheme = this.GetDependencyValues($"{Origin.FolderPath}\\resource\\clientscheme.res", Dependencies, Files);
			this.WriteClientscheme(Origin.Name, NewClientscheme);
			this.CopyHUDFiles(Origin.FolderPath, Files, Dependencies);
			this.WriteInfoVDF();
		}

		/// <summary>
		/// Returns a list of Files and HUD Layout entries that should be used for the merge
		/// </summary>
		private static (HashSet<string> Files, HashSet<string> HUDLayoutEntries, HashSet<string> Events) DestructurePanels(HUDPanel[] Panels)
		{
			HashSet<string> Files = new();
			HashSet<string> HUDLayoutEntries = new();
			HashSet<string> Events = new();
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

				if (Panel.Main.Events != null)
				{
					foreach (string Event in Panel.Main.Events)
					{
						Events.Add(Event);
					}
				}

				if (Panel.Files != null)
				{
					foreach (HUDFile HUDFile in Panel.Files)
					{
						Files.Add(HUDFile.FilePath);
						if (HUDFile.HUDLayout != null)
						{
							foreach (string HUDLayoutEntry in HUDFile.HUDLayout)
							{
								HUDLayoutEntries.Add(HUDLayoutEntry);
							}
						}
						if (HUDFile.Events != null)
						{
							foreach (string Event in HUDFile.Events)
							{
								Events.Add(Event);
							}
						}
					}
				}
			}
			return (Files, HUDLayoutEntries, Events);
		}

		/// <summary>
		/// Returns a set of all clientscheme dependencies used by provided HUD files
		/// </summary>
		private ClientschemeDependencies GetDependencies(string OriginFolderPath, HashSet<string> Files)
		{
			ClientschemeDependencies Dependencies = new();
			Dependencies.HUDPath = OriginFolderPath;

			// Evaluate files requested for merge
			foreach (string HUDFile in Files.ToArray())
			{
				Dependencies.Add(HUDFile, Files);
			}

			return Dependencies;
		}

		/// <summary>
		/// Returns the clientscheme values from a provided set of ClientschemeDependencies
		/// </summary>
		private Dictionary<string, dynamic> GetDependencyValues(string OriginClientschemePath, ClientschemeDependencies Dependencies, HashSet<string> Files)
		{
			Dictionary<string, dynamic> OriginClientscheme = Utilities.LoadAllControls(OriginClientschemePath);

			Dictionary<string, dynamic> NewClientscheme = new();
			NewClientscheme["Colors"] = new Dictionary<string, dynamic>();
			NewClientscheme["Borders"] = new Dictionary<string, dynamic>();
			NewClientscheme["Fonts"] = new Dictionary<string, dynamic>();
			NewClientscheme["CustomFontFiles"] = new Dictionary<string, dynamic>();

			// Borders
			Dictionary<string, dynamic> Borders = OriginClientscheme["Scheme"]["Borders"];
			foreach (string BorderProperty in Dependencies.Borders)
			{
				if (Borders.ContainsKey(BorderProperty))
				{
					NewClientscheme["Borders"][BorderProperty] = Borders[BorderProperty];

					foreach (KeyValuePair<string, dynamic> Property in Borders[BorderProperty])
					{
						if (Property.Key.ToLower().Contains("color"))
						{
							Dependencies.Colours.Add(Property.Value);
						}

						if (Property.Key.ToLower().Contains("image"))
						{
							Files.Add($"materials\\vgui\\{Property.Value}");
						}
					}
				}
			}

			// Colours
			Dictionary<string, dynamic> Colours = OriginClientscheme["Scheme"]["Colors"];
			foreach (string ColourProperty in Dependencies.Colours)
			{
				if (Colours.ContainsKey(ColourProperty))
				{
					NewClientscheme["Colors"][ColourProperty] = Colours[ColourProperty];
				}
			}

			// Fonts
			Dictionary<string, dynamic> OriginFonts = OriginClientscheme["Scheme"]["Fonts"];
			HashSet<string> FontNames = new();

			void AddFontName(Dictionary<string, dynamic> FontDefinition)
			{
				// Example font definition
				// {
				// 	"1"
				// 	{
				// 		"name"			"TF2 Build"
				// 		"tall"			"24"
				// 		"weight"		"500"
				// 		"additive"		"0"
				// 		"antialias"		"1"
				// 	}
				//

				foreach (string FontDefinitionNumber in FontDefinition.Keys)
				{
					// Some HUDs only have a name with an operating system tag like `name ... [$WINDOWS]`
					foreach (KeyValuePair<string, dynamic> FontDefinitionProperty in FontDefinition[FontDefinitionNumber])
					{
						if (FontDefinitionProperty.Key.ToLower().Contains("name"))
						{
							FontNames.Add(FontDefinitionProperty.Value);
						}
					}
				}
			}

			foreach (string FontDefinitionName in Dependencies.Fonts)
			{
				if (OriginFonts.ContainsKey(FontDefinitionName))
				{
					dynamic FontDefinition = OriginFonts[FontDefinitionName];
					NewClientscheme["Fonts"][FontDefinitionName] = FontDefinition;

					// HUD using #base will have multiple font definition number items
					if (FontDefinition.GetType() == typeof(List<dynamic>))
					{
						foreach (Dictionary<string, dynamic> FontDefinitionInstance in FontDefinition)
						{
							AddFontName(FontDefinitionInstance);
						}
					}
					else
					{
						AddFontName(FontDefinition);
					}
				}
			}

			// Add custom fonts
			Dictionary<string, dynamic> OriginalCustomFontFiles = OriginClientscheme["Scheme"]["CustomFontFiles"];
			Dictionary<string, dynamic> ThisCustomFontFiles = Utilities.LoadAllControls(File.Exists($"{this.FolderPath}\\resource\\clientscheme.res") ? $"{this.FolderPath}\\resource\\clientscheme.res" : "Resources\\HUD\\resource\\clientscheme.res");

			int HighestKeyNumber = ((Dictionary<string, dynamic>)ThisCustomFontFiles["Scheme"]["CustomFontFiles"]).Keys.ToArray().Aggregate<string, int>(0, (int a, string b) => Math.Max(a, int.Parse(b)));
			int CustomFontFilesKeysCount = ThisCustomFontFiles["Scheme"]["CustomFontFiles"].Keys.Count + 1;

			int CustomFontFilesIndex = Math.Max(HighestKeyNumber, CustomFontFilesKeysCount) + 1;

			foreach (KeyValuePair<string, dynamic> CustomFontFile in OriginalCustomFontFiles)
			{
				foreach (string FontName in FontNames)
				{
					if (CustomFontFile.Value.GetType() == typeof(Dictionary<string, dynamic>))
					{
						Dictionary<string, dynamic> CustomFontFileValue = CustomFontFile.Value;
						if (CustomFontFileValue["name"] == FontName)
						{
							// Normal custom font implementation
							NewClientscheme["CustomFontFiles"][$"{CustomFontFilesIndex}"] = CustomFontFile.Value;
							CustomFontFilesIndex++;

							// Add all properties where the key is or includes 'font'
							//
							// Example of using OS tags (from HexHUD/Faiths HUD https://huds.tf/site/s-Faith-s-HUD)
							// "1"
							// {
							// 	"font" "resource/scheme/fonts/Renogare.ttf" [$WINDOWS]
							// 	"font" "resource/scheme/fonts/linux/Renogare Linux.otf" [$POSIX]
							// 	"name" "Renogare Soft"
							// }
							// This gets parsed as
							// "1": {
							// 	"font^[$WINDOWS]": "resource/scheme/fonts/linux/Renogare Linux.otf",
							// 	"font^[$POSIX]": "esource/scheme/fonts/linux/Renogare Linux.otf",
							// 	"name": "Renogare Soft"
							// }

							foreach (KeyValuePair<string, dynamic> Property in CustomFontFileValue)
							{
								if (Property.Key.Contains("font"))
								{
									Files.Add(Property.Value);
								}
							}
						}
					}
					else if (CustomFontFile.Value.GetType() == typeof(string))
					{
						// 'Guess' whether the font is used by checking if the file path contains the name of the font
						if (CustomFontFile.Value.Contains(FontName))
						{
							// Assume HUD developer only references font once, so dont do any
							// more checking as to whether or not we have already added the
							// customfontfile definition to the new clientscheme

							NewClientscheme["CustomFontFiles"][$"{CustomFontFilesIndex}"] = CustomFontFile.Value;
							CustomFontFilesIndex++;
							Files.Add(CustomFontFile.Value);
						}
					}
				}
			}

			return NewClientscheme;
		}

		private void WriteHUDLayout(string OriginFolderPath, HashSet<string> HUDLayoutEntries, ClientschemeDependencies Dependencies, HashSet<string> Files)
		{
			string OriginHUDLayoutPath = $"{OriginFolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> OriginHUDLayout = new();

			void AddControls(string FilePath, bool Base)
			{
				Dictionary<string, dynamic> Obj = File.Exists(FilePath) ? Utilities.VDFTryParse(FilePath) : new()
				{
					["Resource/HudLayout.res"] = new Dictionary<string, dynamic>()
				};

				// #base
				if (Obj.ContainsKey("#base"))
				{
					List<string> BaseFiles = new();
					if (Obj["#base"].GetType() == typeof(List<dynamic>))
					{
						foreach (dynamic BaseFile in Obj["#base"])
						{
							BaseFiles.Add(BaseFile);
						}
					}
					else
					{
						BaseFiles.Add(Obj["#base"]);
					}

					string[] Folders = FilePath.Split("\\");
					// Remove File Name
					Folders[^1] = "";
					foreach (string BaseFile in BaseFiles)
					{
						AddControls(String.Join('\\', Folders) + BaseFile, true);
					}
				}

				// Merge
				foreach (string ContainerKey in Obj.Keys)
				{
					if (Obj[ContainerKey].GetType() == typeof(List<dynamic>))
					{
						foreach (dynamic Item in Obj[ContainerKey])
						{
							if (Item.GetType() == typeof(Dictionary<string, dynamic>))
							{
								foreach (string HUDLayoutEntryKey in Item.Keys)
								{
									if (OriginHUDLayout.ContainsKey(HUDLayoutEntryKey))
									{
										if (!Base)
										{
											OriginHUDLayout[HUDLayoutEntryKey] = Item[HUDLayoutEntryKey];
										}
									}
									else
									{
										OriginHUDLayout[HUDLayoutEntryKey] = Item[HUDLayoutEntryKey];
									}
								}
							}
							else
							{
								// There shouldn't be a top layer string hudlayout.res
							}
						}
					}
					else if (Obj[ContainerKey].GetType() == typeof(Dictionary<string, dynamic>))
					{
						foreach (string HUDLayoutEntryKey in Obj[ContainerKey].Keys)
						{
							if (OriginHUDLayout.ContainsKey(HUDLayoutEntryKey))
							{
								if (!Base)
								{
									OriginHUDLayout[HUDLayoutEntryKey] = Obj[ContainerKey][HUDLayoutEntryKey];
								}
							}
							else
							{
								OriginHUDLayout[HUDLayoutEntryKey] = Obj[ContainerKey][HUDLayoutEntryKey];
							}
						}
					}
					else
					{
						// There shouldn't be a top layer string that is not #base in hudlayout.res
					}
				}
			}

			AddControls(OriginHUDLayoutPath, false);

			string ThisHUDLayoutPath = $"{this.FolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> NewHUDLayout = Utilities.VDFTryParse(File.Exists(ThisHUDLayoutPath) ? ThisHUDLayoutPath : "Resources\\HUD\\scripts\\hudlayout.res");

			if (!NewHUDLayout.ContainsKey("Resource/HudLayout.res"))
			{
				NewHUDLayout["Resource/HudLayout.res"] = new Dictionary<string, dynamic>();
			}

			foreach (string HUDLayoutEntry in HUDLayoutEntries)
			{
				if (OriginHUDLayout.ContainsKey(HUDLayoutEntry))
				{
					NewHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry] = OriginHUDLayout[HUDLayoutEntry];
					Dependencies.Add("scripts", OriginHUDLayout[HUDLayoutEntry], Files);
				}
			}

			Directory.CreateDirectory($"{this.FolderPath}\\scripts");
			File.WriteAllText($"{this.FolderPath}\\scripts\\hudlayout.res", VDF.Stringify(NewHUDLayout));

			Files.Remove("scripts\\hudlayout.res");
		}

		/// <summary>
		/// Copies HUD Animations from origin HUD, adds clientscheme variables to provided Dependencies object
		/// </summary>
		private void WriteHUDAnimations(string OriginFolderPath, HashSet<string> Events, string HUDName, ClientschemeDependencies Dependencies, HashSet<string> Files)
		{
			if (Events.Count == 0)
			{
				return;
			}

			string OriginHUDAnimationsManifestPath = OriginFolderPath + "\\scripts\\hudanimations_manifest.txt";

			Dictionary<string, dynamic> Manifest = Utilities.VDFTryParse(Utilities.TestPath(OriginHUDAnimationsManifestPath) ? OriginHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
			List<string> HUDAnimationsManifestList = (List<string>)Manifest["hudanimations_manifest"]["file"];

			Dictionary<string, List<HUDAnimation>> NewHUDAnimations = new();

			foreach (string FilePath in HUDAnimationsManifestList)
			{
				if (File.Exists(OriginFolderPath + "\\" + FilePath))
				{
					Dictionary<string, List<HUDAnimation>> AnimationsFile = HUDAnimations.Parse(File.ReadAllText(OriginFolderPath + "\\" + FilePath));


					// Add clientscheme colours and sounds referenced in HUD animations
					void AddEventDependencies(string Event, List<HUDAnimation> AnimationEvent)
					{
						NewHUDAnimations[Event] = AnimationEvent;

						foreach (dynamic Statement in AnimationEvent)
						{
							Type T = Statement.GetType();

							if (T == typeof(Animate))
							{
								if (Statement.Property.ToLower().Contains("color"))
								{
									// System.Diagnostics.Debug.WriteLine("adding " + Statement.Value);
									Dependencies.Colours.Add(Statement.Value);
								}
							}

							if (T == typeof(RunEvent) || T == typeof(StopEvent) || T == typeof(RunEventChild))
							{
								// If the event called has not been evaluated, evaluate in
								// case there are more dependencies that need to be added
								if (!NewHUDAnimations.ContainsKey(Statement.Event) && AnimationsFile.ContainsKey(Event))
								{
									AddEventDependencies(Statement.Event, AnimationsFile[Event]);
								}
							}

							if (T == typeof(PlaySound))
							{
								Files.Add("sound\\" + Statement.Sound);
							}
						}
					}

					foreach (string Event in AnimationsFile.Keys)
					{
						if (Events.Contains(Event))
						{
							AddEventDependencies(Event, AnimationsFile[Event]);
						}
					}
				}

			}

			string HUDAnimationsPath = $"{this.FolderPath}\\scripts\\hudanimations_{HUDName}.txt";
			Directory.CreateDirectory(Path.GetDirectoryName(HUDAnimationsPath));
			File.WriteAllText(HUDAnimationsPath, HUDAnimations.Stringify(NewHUDAnimations));

			// Include origin animations file (Fake stringify VDF because we want to put the new animations file at the start to overwrite default tf2 animations)
			List<string> NewManifestLines = new()
			{
				$"hudanimations_manifest",
				$"{{",
			};

			if (!HUDAnimationsManifestList.Contains($"scripts/hudanimations_{HUDName}.txt"))
			{
				NewManifestLines.Add($"\t\"file\"\t\t\"scripts/hudanimations_{HUDName}.txt\"");
			}

			string TargetHUDAnimationsManifestPath = $"{this.FolderPath}\\scripts\\hudanimations_manifest.txt";

			Dictionary<string, dynamic> TargetManifest = Utilities.VDFTryParse(File.Exists(TargetHUDAnimationsManifestPath) ? TargetHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
			dynamic TargetHUDAnimationsManifestList = TargetManifest["hudanimations_manifest"]["file"];

			foreach (string FilePath in TargetHUDAnimationsManifestList)
			{
				NewManifestLines.Add($"\t\"file\"\t\t\"{FilePath}\"");
			}

			NewManifestLines.Add($"}}");

			File.WriteAllLines($"{this.FolderPath}\\scripts\\hudanimations_manifest.txt", NewManifestLines);
		}

		/// <summary>
		/// Applies NewClientscheme to this HUD using #base
		/// </summary>
		private void WriteClientscheme(string OriginName, Dictionary<string, dynamic> NewClientscheme)
		{
			bool WriteBaseStatement = true;
			if (Utilities.TestPath($"{this.FolderPath}\\resource\\clientscheme.res"))
			{
				string[] Lines = File.ReadAllLines($"{this.FolderPath}\\resource\\clientscheme.res");
				int i = 0;
				while (WriteBaseStatement && i < Lines.Length)
				{
					if (Lines[i].Contains($"clientscheme_{OriginName}.res"))
					{
						WriteBaseStatement = false;
					}
					i++;
				}
			}
			else
			{
				// If clientscheme doesn't exist it is crucial to have one with default tf properties
				Directory.CreateDirectory($"{this.FolderPath}\\resource");
				File.Copy("Resources\\HUD\\resource\\clientscheme.res", $"{this.FolderPath}\\resource\\clientscheme.res");
			}

			if (WriteBaseStatement)
			{
				File.AppendAllLines($"{this.FolderPath}\\resource\\clientscheme.res", new string[]
				{
					"",
					$"\"#base\"\t\t\"clientscheme_{OriginName}.res\""
				});
			}

			// ALWAYS create new clientscheme.res

			// Remove empty clientscheme sections
			foreach (string Key in NewClientscheme.Keys)
			{
				if (NewClientscheme[Key].Keys.Count == 0)
				{
					NewClientscheme.Remove(Key);
				}
			}
			Dictionary<string, dynamic> NewClientschemeContainer = new();
			NewClientschemeContainer["Scheme"] = NewClientscheme;
			Directory.CreateDirectory($"{this.FolderPath}\\resource");

			string ClientschemeDependenciesPath = $"{this.FolderPath}\\resource\\clientscheme_{OriginName}.res";

			if (Utilities.TestPath(ClientschemeDependenciesPath))
			{
				Dictionary<string, dynamic> PreviouslyMergedClientscheme = Utilities.VDFTryParse(ClientschemeDependenciesPath);
				Utilities.Merge(PreviouslyMergedClientscheme, NewClientschemeContainer);
				File.WriteAllText(ClientschemeDependenciesPath, VDF.Stringify(PreviouslyMergedClientscheme));
			}
			else
			{
				File.WriteAllText(ClientschemeDependenciesPath, VDF.Stringify(NewClientschemeContainer));
			}
		}

		/// <summary>
		/// Copies list of HUD Files with no processing
		/// </summary>
		private void CopyHUDFiles(string OriginFolderPath, HashSet<string> Files, ClientschemeDependencies Dependencies)
		{
			foreach (string ImagePath in Dependencies.Images)
			{
				string[] Folders = Regex.Split(ImagePath, "[\\/]+");

				Files.Add($"materials\\vgui\\{String.Join("\\", Folders)}.vmt");
				Files.Add($"materials\\vgui\\{String.Join("\\", Folders)}.vtf");

				string FilePath = $"{OriginFolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vmt";
				if (File.Exists(FilePath))
				{
					Dictionary<string, dynamic> VMT = Utilities.VDFTryParse(FilePath, false);
					Dictionary<string, dynamic> Generic = VMT.First().Value;

					string VTFPath = "";
					int i = 0;

					while (VTFPath == "" && i < Generic.Keys.Count)
					{
						if (Generic.ElementAt(i).Key.ToLower().Contains("basetexture"))
						{
							VTFPath = Generic.ElementAt(i).Value;
						}
						i++;
					}

					Files.Add("materials\\" + String.Join("\\", Regex.Split(VTFPath, "[\\/]+")));
				}
			}

			foreach (string AudioPath in Dependencies.Audio)
			{
				string[] Folders = Regex.Split(AudioPath, "[\\\\/]+");
				Files.Add($"sound\\{String.Join("\\", Folders)}");
			}

			string[] FilesArray = Files.ToArray();
			foreach (string FilePath in FilesArray)
			{
				string SourceFileName = $"{OriginFolderPath}\\{FilePath}";
				if (File.Exists(SourceFileName))
				{
					string DestFileName = $"{this.FolderPath}\\{FilePath}";
					Directory.CreateDirectory(Path.GetDirectoryName(DestFileName));
					File.Copy(SourceFileName, DestFileName, true);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Could not find {SourceFileName}");
				}
			}
		}

		/// <summary>
		/// Writes an info.vdf file to the current HUD if it doesn't exist
		/// </summary>
		private void WriteInfoVDF()
		{
			// UI Version
			if (!File.Exists($"{this.FolderPath}\\info.vdf"))
			{
				Dictionary<string, dynamic> InfoVDF = new();
				InfoVDF[this.Name] = new Dictionary<string, dynamic>();
				InfoVDF[this.Name]["ui_version"] = 3;
				File.WriteAllText($"{this.FolderPath}\\info.vdf", VDF.Stringify(InfoVDF));
			}
		}
	}
}