using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
			string[] Folders = FolderPath.Split("\\");
			this.Name = Folders[Folders.Length - 1];
			this.FolderPath = FolderPath;
		}

		/// <summary>Returns whether the provided HUDPanel is 'in' this HUD</summary>
		public bool TestPanel(HUDPanel Panel)
		{
			return File.Exists(this.FolderPath + "\\" + Panel.Main.FilePath);
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
			this.WriteHUDAnimations(Origin.FolderPath, Events, Origin.Name, Dependencies, Files);
			Dictionary<string, dynamic> NewClientscheme = this.GetDependencyValues(Origin.FolderPath + "\\resource\\clientscheme.res", Dependencies, Files);
			this.WriteHUDLayout(Origin.FolderPath + "\\scripts\\hudlayout.res", HUDLayoutEntries);
			this.WriteClientscheme(Origin.Name, NewClientscheme);
			this.CopyHUDFiles(Origin.FolderPath, Files, Dependencies);
			this.WriteInfoVDF();

			string LogPath = this.FolderPath + "\\merge.log";

			File.AppendAllLines(LogPath, new string[] { $"Merge at {DateTime.Now}", Origin.Name, Origin.FolderPath, });

			File.AppendAllLines(LogPath, new string[] { "", "Panels:", "" });
			File.AppendAllLines(LogPath, Panels.Select((i) => i.Name));

			File.AppendAllLines(LogPath, new string[] { "", "Files:", "" });
			File.AppendAllLines(LogPath, Files);

			File.AppendAllLines(LogPath, new string[] { "", "HUD Layout Entries:", "" });
			File.AppendAllLines(LogPath, HUDLayoutEntries);

			File.AppendAllLines(LogPath, new string[] { "", "Events:", "" });
			File.AppendAllLines(LogPath, Events);
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
			ClientschemeDependencies Properties = JsonSerializer.Deserialize<ClientschemeDependencies>(File.ReadAllText("Resources\\Clientscheme.json"));
			ClientschemeDependencies Dependencies = new();

			void AddDependencies(string HUDFile)
			{
				string SourceFilePath = OriginFolderPath + "\\" + HUDFile;
				if (!File.Exists(SourceFilePath))
				{
					System.Diagnostics.Debug.WriteLine("Could not find " + SourceFilePath);
				}
				Dictionary<string, dynamic> Obj = File.Exists(SourceFilePath) ? Utilities.VDFTryParse(SourceFilePath) : new();

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

					// Get the current location of HUDFile
					string[] Folders = HUDFile.Split("\\");
					// Remove File Name
					Folders[Folders.Length - 1] = "";
					foreach (string BaseFile in BaseFiles)
					{
						string BaseFilePath = String.Join('\\', Regex.Split(BaseFile, "[\\/]+"));
						Files.Add(String.Join('\\', Folders) + "\\" + BaseFilePath);
						AddDependencies(String.Join('\\', Folders) + BaseFilePath);
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

							foreach (System.Reflection.PropertyInfo TypeKey in T.GetProperties())
							{
								dynamic CurrentPropertiesList = TypeKey.GetValue(Properties);
								HashSet<string> CurrentDependenciesList = TypeKey.GetValue(Dependencies) as HashSet<string>;

								foreach (string DefaultPropertyKey in CurrentPropertiesList)
								{
									if (Key == DefaultPropertyKey)
									{
										if (Obj[Key].GetType() == typeof(List<dynamic>))
										{
											foreach (dynamic DuplicateKey in Obj[Key])
											{
												CurrentDependenciesList.Add(DuplicateKey);
											}
										}
										else
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

			// Evaluate files requested for merge
			foreach (string HUDFile in Files.ToArray())
			{
				string SourceFileName = OriginFolderPath + "\\" + HUDFile;
				if (File.Exists(SourceFileName))
				{
					AddDependencies(HUDFile);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Could not find " + SourceFileName);
				}
			}

			return Dependencies;
		}

		/// <summary>
		/// Returns the clientscheme values from a provided set of ClientschemeDependencies
		/// </summary>
		private Dictionary<string, dynamic> GetDependencyValues(string OriginClientschemePath, ClientschemeDependencies Dependencies, HashSet<string> Files)
		{
			Dictionary<string, dynamic> OriginClientscheme = Utilities.LoadControls(OriginClientschemePath);

			Dictionary<string, dynamic> NewClientscheme = new();
			NewClientscheme["Colors"] = new Dictionary<string, dynamic>();
			NewClientscheme["Borders"] = new Dictionary<string, dynamic>();
			NewClientscheme["Fonts"] = new Dictionary<string, dynamic>();
			NewClientscheme["CustomFontFiles"] = new Dictionary<string, dynamic>();

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
					NewClientscheme["Borders"][BorderProperty] = OriginClientscheme["Scheme"]["Borders"][BorderProperty];
				}
			}

			// Fonts
			HashSet<string> FontNames = new();
			foreach (string FontProperty in Dependencies.Fonts)
			{
				if (OriginClientscheme?["Scheme"]?["Fonts"].ContainsKey(FontProperty))
				{
					dynamic FontDefinition = OriginClientscheme["Scheme"]["Fonts"][FontProperty];
					NewClientscheme["Fonts"][FontProperty] = FontDefinition;

					// HUD using #base will have multiple font definition number items
					if (FontDefinition.GetType() == typeof(List<object>))
					{
						foreach (Dictionary<string, dynamic> FontDefinitionInstance in FontDefinition)
						{
							foreach (KeyValuePair<string, dynamic> FontDefinitionKV in FontDefinitionInstance)
							{
								if (FontDefinitionKV.Value.GetType() == typeof(Dictionary<string, dynamic>))
								{
									foreach (KeyValuePair<string, dynamic> FontDefinitionProperty in FontDefinitionKV.Value)
									{
										if (FontDefinitionProperty.Key.ToLower().Contains("name"))
										{
											FontNames.Add(FontDefinitionProperty.Value);
										}
									}
								}
							}
						}
					}
					else
					{
						foreach (string FontDefinitionNumber in FontDefinition.Keys)
						{
							foreach (string FontDefinitionProperty in FontDefinition?[FontDefinitionNumber]?.Keys)
							{
								// Some HUDs only have a name with an operating system tag like `name ... [$WINDOWS]`
								if (FontDefinitionProperty.ToLower().Contains("name"))
								{
									FontNames.Add(FontDefinition?[FontDefinitionNumber]?[FontDefinitionProperty]);
								}
							}
						}
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
					if (OriginalCustomFontFiles?[CustomFontFileNumber].GetType() == typeof(Dictionary<string, dynamic>))
					{
						if (FontName == OriginalCustomFontFiles?[CustomFontFileNumber]?["name"])
						{
							NewClientscheme["CustomFontFiles"].Add(CustomFontFileNumber, OriginalCustomFontFiles?[CustomFontFileNumber]);

							// Add .ttf file as well

							// add properties that include 'font', for if HUD only has font%[$WIN32] and font%[$OSX] or like
							foreach (KeyValuePair<string, dynamic> property in OriginalCustomFontFiles?[CustomFontFileNumber])
							{
								if (property.Key.ToLower().Contains("font"))
								{
									Files.Add(property.Value);
								}
							}
						}
					}
				}
			}

			return NewClientscheme;
		}

		private void WriteHUDLayout(string OriginHUDLayoutPath, HashSet<string> HUDLayoutEntries)
		{
			Dictionary<string, dynamic> OriginHUDLayout = Utilities.LoadControls(OriginHUDLayoutPath)["Resource/HudLayout.res"];

			string ThisHUDLayoutPath = $"{this.FolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> NewHUDLayout = Utilities.VDFTryParse(File.Exists(ThisHUDLayoutPath) ? ThisHUDLayoutPath : "Resources\\HUD\\scripts\\hudlayout.res");


			foreach (string HUDLayoutEntry in HUDLayoutEntries)
			{
				if (OriginHUDLayout.ContainsKey(HUDLayoutEntry))
				{
					NewHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry] = OriginHUDLayout[HUDLayoutEntry];
				}
				else
				{
					// System.Diagnostics.Debug.WriteLine($"{Origin.Name}'s hudlayout does not contain {HUDLayoutEntry}!");
				}
			}

			Directory.CreateDirectory($"{this.FolderPath}\\scripts");
			File.WriteAllText($"{this.FolderPath}\\scripts\\hudlayout.res", VDF.Stringify(NewHUDLayout));
		}

		/// <summary>
		/// Copies HUD Animations from origin HUD, adds clientscheme variables to provided Dependencies object
		/// </summary>
		private void WriteHUDAnimations(string OriginFolderPath, HashSet<string> Events, string HUDName, ClientschemeDependencies Dependencies, HashSet<string> Files)
		{
			string OriginHUDAnimationsManifestPath = OriginFolderPath + "\\scripts\\hudanimations_manifest.txt";

			Dictionary<string, dynamic> Manifest = Utilities.VDFTryParse(File.Exists(OriginHUDAnimationsManifestPath) ? OriginHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
			dynamic HUDAnimationsManifestList = Manifest["hudanimations_manifest"]["file"];

			Dictionary<string, List<HUDAnimation>> NewHUDAnimations = new();

			foreach (string FilePath in HUDAnimationsManifestList)
			{
				if (File.Exists(OriginFolderPath + "\\" + FilePath))
				{
					Dictionary<string, List<HUDAnimation>> AnimationsFile = HUDAnimations.Parse(File.ReadAllText(OriginFolderPath + "\\" + FilePath));

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
					$"\"#base\" \"clientscheme_{OriginName}.res\""
				});
			}

			// ALWAYS create new clientscheme.res
			Dictionary<string, dynamic> NewClientschemeContainer = new();
			NewClientschemeContainer["Scheme"] = NewClientscheme;
			Directory.CreateDirectory($"{this.FolderPath}\\resource");

			string ClientschemeDependenciesPath = $"{this.FolderPath}\\resource\\clientscheme_{OriginName}.res";

			if (File.Exists(ClientschemeDependenciesPath))
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

				try
				{
					string FilePath = $"{OriginFolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vmt";
					if (File.Exists(FilePath))
					{
						Dictionary<string, dynamic> VMT = Utilities.VDFTryParse(FilePath);
						Files.Add("materials\\" + String.Join("\\", Regex.Split(VMT["UnlitGeneric"]["$basetexture"], "[\\/]+")));
					}
				}
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine(e.Message);
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