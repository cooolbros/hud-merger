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

			(List<string> Files, List<string> HUDLayoutEntries) = HUD.DestructurePanels(Panels);
			ClientschemeDependencies Dependencies = this.GetDependencies(Origin.FolderPath, Files.ToArray());
			Dictionary<string, dynamic> NewClientscheme = this.GetDependencyValues(Origin.FolderPath + "\\resource\\clientscheme.res", Dependencies, Files);
			this.WriteClientscheme(Origin.Name, NewClientscheme);
			this.WriteHUDLayout(Origin.FolderPath + "\\scripts\\hudlayout.res", HUDLayoutEntries);
			this.CopyHUDFiles(Origin.FolderPath, Files, Dependencies);
			this.WriteInfoVDF();
		}

		/// <summary>
		/// Returns a list of Files and HUD Layout entries that should be used for the merge
		/// </summary>
		private static (List<string> Files, List<string> HUDLayoutEntries) DestructurePanels(HUDPanel[] Panels)
		{
			List<string> Files = new();
			List<string> HUDLayoutEntries = new();
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
			return (Files, HUDLayoutEntries);
		}

		/// <summary>
		/// Returns a set of all clientscheme dependencies used by provided HUD files
		/// </summary>
		private ClientschemeDependencies GetDependencies(string OriginFolderPath, string[] Files)
		{
			ClientschemeDependencies Properties = JsonSerializer.Deserialize<ClientschemeDependencies>(File.ReadAllText("Resources\\Clientscheme.json"));
			ClientschemeDependencies Dependencies = new();

			void AddDependencies(string FilePath)
			{
				Dictionary<string, dynamic> Obj = File.Exists(FilePath) ? VDF.Parse(File.ReadAllText(FilePath)) : new();

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
							Type T = typeof(ClientschemeDependencies);

							foreach (System.Reflection.PropertyInfo TypeKey in T.GetProperties())
							{
								dynamic CurrentPropertiesList = TypeKey.GetValue(Properties);
								dynamic CurrentDependenciesList = TypeKey.GetValue(Dependencies) as HashSet<string>;

								foreach (string DefaultPropertyKey in CurrentPropertiesList)
								{
									if (Key == DefaultPropertyKey)
									{
										// dynamic Value = TypeKey.GetValue(Dependencies);

										if (Obj[Key].GetType().Name.Contains("List"))
										{
											// Prompt user to decide which option to use (or pick 0?)

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
			foreach (string HUDFile in Files)
			{
				string SourceFileName = OriginFolderPath + "\\" + HUDFile;
				if (File.Exists(SourceFileName))
				{
					AddDependencies(SourceFileName);
				}
			}

			return Dependencies;
		}

		/// <summary>
		/// Returns the clientscheme values from a provided set of ClientschemeDependencies
		/// </summary>
		private Dictionary<string, dynamic> GetDependencyValues(string OriginClientschemePath, ClientschemeDependencies Dependencies, List<string> Files)
		{
			Dictionary<string, dynamic> OriginClientscheme = Utilities.LoadControls(OriginClientschemePath);

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

			return NewClientscheme;
		}

		private void WriteHUDLayout(string OriginHUDLayoutPath, List<string> HUDLayoutEntries)
		{
			Dictionary<string, dynamic> OriginHUDLayout = Utilities.LoadControls(OriginHUDLayoutPath);

			string ThisHUDLayoutPath = $"{this.FolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> NewHUDLayout = Utilities.LoadControls(File.Exists(ThisHUDLayoutPath) ? ThisHUDLayoutPath : "Resources\\hudlayout.res");

			foreach (string HUDLayoutEntry in HUDLayoutEntries)
			{
				if (OriginHUDLayout?["Resource/HudLayout.res"].ContainsKey(HUDLayoutEntry))
				{
					NewHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry] = OriginHUDLayout["Resource/HudLayout.res"][HUDLayoutEntry];
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
		/// Applies NewClientscheme to this HUD using #base
		/// </summary>
		private void WriteClientscheme(string OriginName, Dictionary<string, dynamic> NewClientscheme)
		{
			bool WriteBaseStatement = true;
			if (File.Exists($"{this.FolderPath}\\resource\\clientscheme.res"))
			{
				// 'this' HUD already has a hudlayout
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
				File.Copy("Resources\\clientscheme.res", $"{this.FolderPath}\\resource\\clientscheme.res");
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
			File.WriteAllText($"{this.FolderPath}\\resource\\clientscheme_{OriginName}.res", VDF.Stringify(NewClientschemeContainer));
		}

		private void CopyHUDFiles(string OriginFolderPath, List<string> Files, ClientschemeDependencies Dependencies)
		{
			foreach (string ImagePath in Dependencies.Images)
			{
				string[] Folders = System.Text.RegularExpressions.Regex.Split(ImagePath, "[\\\\/]+");
				Files.Add($"{OriginFolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vmt");
				Files.Add($"{OriginFolderPath}\\materials\\vgui\\{String.Join("\\", Folders)}.vtf");
			}

			foreach (string AudioPath in Dependencies.Audio)
			{
				string[] Folders = System.Text.RegularExpressions.Regex.Split(AudioPath, "[\\\\/]+");
				Files.Add($"{OriginFolderPath}\\sound\\{String.Join("\\", Folders)}");
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