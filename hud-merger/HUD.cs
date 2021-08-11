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

		public HUD(string folderPath)
		{
			this.Name = folderPath.Split('\\')[^1];
			this.FolderPath = folderPath;
		}

		/// <summary>Returns whether the provided HUDPanel is 'in' this HUD</summary>
		public bool TestPanel(HUDPanel panel)
		{
			return Utilities.TestPath($"{this.FolderPath}\\{panel.Main.FilePath}");
		}

		/// <summary>Merges an array of HUDPanels from another HUD into this HUD</summary>
		public void Merge(HUD origin, HUDPanel[] panels)
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

			(FilesHashSet files, HashSet<string> hudLayoutEntries, HashSet<string> events) = HUD.DestructurePanels(panels);
			ClientschemeDependencies dependencies = this.GetDependencies(origin.FolderPath, files);
			this.WriteHUDLayout(origin.FolderPath, hudLayoutEntries, dependencies, files);
			this.WriteHUDAnimations(origin.FolderPath, events, origin.Name, dependencies, files);
			Dictionary<string, dynamic> newClientscheme = this.GetDependencyValues($"{origin.FolderPath}\\resource\\clientscheme.res", dependencies, files);
			this.WriteClientscheme(origin.Name, newClientscheme);
			this.CopyHUDFiles(origin.FolderPath, files, dependencies);
			this.WriteInfoVDF();
		}

		/// <summary>
		/// Returns a list of Files and HUD Layout entries that should be used for the merge
		/// </summary>
		private static (FilesHashSet files, HashSet<string> hudLayoutEntries, HashSet<string> events) DestructurePanels(HUDPanel[] panels)
		{
			FilesHashSet files = new FilesHashSet();
			HashSet<string> hudLayoutEntries = new();
			HashSet<string> events = new();
			foreach (HUDPanel panel in panels)
			{
				files.Add(panel.Main.FilePath);

				if (panel.Main.HUDLayout != null)
				{
					foreach (string hudLayoutEntry in panel.Main.HUDLayout)
					{
						hudLayoutEntries.Add(hudLayoutEntry);
					}
				}

				if (panel.Main.Events != null)
				{
					foreach (string @event in panel.Main.Events)
					{
						events.Add(@event);
					}
				}

				if (panel.Files != null)
				{
					foreach (HUDFile hudFile in panel.Files)
					{
						files.Add(hudFile.FilePath);
						if (hudFile.HUDLayout != null)
						{
							foreach (string hudLayoutEntry in hudFile.HUDLayout)
							{
								hudLayoutEntries.Add(hudLayoutEntry);
							}
						}
						if (hudFile.Events != null)
						{
							foreach (string Event in hudFile.Events)
							{
								events.Add(Event);
							}
						}
					}
				}
			}
			return (files, hudLayoutEntries, events);
		}

		/// <summary>
		/// Returns a set of all clientscheme dependencies used by provided HUD files
		/// </summary>
		private ClientschemeDependencies GetDependencies(string originFolderPath, FilesHashSet files)
		{
			ClientschemeDependencies dependencies = new();
			dependencies.HUDPath = originFolderPath;

			// Evaluate files requested for merge
			foreach (string hudFile in files.ToArray())
			{
				dependencies.Add(hudFile, files);
			}

			return dependencies;
		}

		/// <summary>
		/// Returns the clientscheme values from a provided set of ClientschemeDependencies
		/// </summary>
		private Dictionary<string, dynamic> GetDependencyValues(string originClientschemePath, ClientschemeDependencies dependencies, FilesHashSet files)
		{
			Dictionary<string, dynamic> originClientscheme = Utilities.LoadAllControls(originClientschemePath);

			Dictionary<string, dynamic> newClientscheme = new();
			newClientscheme["Colors"] = new Dictionary<string, dynamic>();
			newClientscheme["Borders"] = new Dictionary<string, dynamic>();
			newClientscheme["Fonts"] = new Dictionary<string, dynamic>();
			newClientscheme["CustomFontFiles"] = new Dictionary<string, dynamic>();

			// Borders
			Dictionary<string, dynamic> borders = originClientscheme["Scheme"]["Borders"];
			foreach (string borderProperty in dependencies.Borders)
			{
				if (borders.ContainsKey(borderProperty))
				{
					newClientscheme["Borders"][borderProperty] = borders[borderProperty];

					foreach (KeyValuePair<string, dynamic> property in borders[borderProperty])
					{
						if (property.Key.ToLower().Contains("color"))
						{
							dependencies.Colours.Add(property.Value);
						}

						if (property.Key.ToLower().Contains("image"))
						{
							dependencies.Images.Add($"materials\\vgui\\{property.Value}");
						}
					}
				}
			}

			// Colours
			Dictionary<string, dynamic> colours = originClientscheme["Scheme"]["Colors"];
			foreach (string colourProperty in dependencies.Colours)
			{
				if (colours.ContainsKey(colourProperty))
				{
					newClientscheme["Colors"][colourProperty] = colours[colourProperty];
				}
			}

			// Fonts
			Dictionary<string, dynamic> originFonts = originClientscheme["Scheme"]["Fonts"];
			HashSet<string> fontNames = new();

			void AddFontName(Dictionary<string, dynamic> fontDefinition)
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

				foreach (string fontDefinitionNumber in fontDefinition.Keys)
				{
					// Some HUDs only have a name with an operating system tag like `name ... [$WINDOWS]`
					foreach (KeyValuePair<string, dynamic> fontDefinitionProperty in fontDefinition[fontDefinitionNumber])
					{
						if (fontDefinitionProperty.Key.ToLower().Contains("name"))
						{
							fontNames.Add(fontDefinitionProperty.Value);
						}
					}
				}
			}

			foreach (string fontDefinitionName in dependencies.Fonts)
			{
				if (originFonts.ContainsKey(fontDefinitionName))
				{
					dynamic fontDefinition = originFonts[fontDefinitionName];
					newClientscheme["Fonts"][fontDefinitionName] = fontDefinition;

					// HUD using #base will have multiple font definition number items
					if (fontDefinition.GetType() == typeof(List<dynamic>))
					{
						foreach (Dictionary<string, dynamic> fontDefinitionInstance in fontDefinition)
						{
							AddFontName(fontDefinitionInstance);
						}
					}
					else
					{
						AddFontName(fontDefinition);
					}
				}
			}

			// Add custom fonts
			Dictionary<string, dynamic> originalCustomFontFiles = originClientscheme["Scheme"]["CustomFontFiles"];

			// this clientscheme
			Dictionary<string, dynamic> thisCustomFontFiles = Utilities.LoadAllControls(File.Exists($"{this.FolderPath}\\resource\\clientscheme.res") ? $"{this.FolderPath}\\resource\\clientscheme.res" : "Resources\\HUD\\resource\\clientscheme.res")["Scheme"]["CustomFontFiles"];

			// Values and hashes for this hud
			Dictionary<string, dynamic> fontFileDefinitions = new();

			// list of hashes of fonts refereneced on fontNames
			List<string> referencedCustomFontFileDefinitions = new();

			string HashCustomFontFileDefinition(Dictionary<string, dynamic> customFontFile)
			{
				List<string> properties = new();
				foreach (KeyValuePair<string, dynamic> property in customFontFile)
				{
					if (property.Value.GetType() == typeof(Dictionary<string, dynamic>))
					{
						properties.Add(HashCustomFontFileDefinition(property.Value));
					}
					else
					{
						properties.Add(FilesHashSet.EncodeFilePath(property.Value));
					}
				}
				properties.Sort();
				return String.Join('_', properties);
			}

			// Create hashes for fonts that exist in this hud
			foreach (KeyValuePair<string, dynamic> customFontFile in thisCustomFontFiles)
			{
				if (customFontFile.Value.GetType() == typeof(Dictionary<string, dynamic>))
				{
					fontFileDefinitions[HashCustomFontFileDefinition(customFontFile.Value)] = customFontFile.Value;
				}
				else if (customFontFile.Value.GetType() == typeof(string))
				{
					fontFileDefinitions[HashCustomFontFileDefinition(new Dictionary<string, dynamic>()
					{
						["font"] = customFontFile.Value
					})] = customFontFile.Value;
				}
			}

			// Add referenced fonts to fontFileDefinitions and referencedCustomFontFileDefinitions
			// if they arent referenced by this hud already
			foreach (KeyValuePair<string, dynamic> customFontFile in originalCustomFontFiles)
			{
				foreach (string fontName in fontNames)
				{
					if (customFontFile.Value.GetType() == typeof(Dictionary<string, dynamic>))
					{
						Dictionary<string, dynamic> customFontFileValue = customFontFile.Value;
						if (customFontFileValue["name"] == fontName)
						{
							// Normal custom font implementation
							string customFontFileDefinitionHash = HashCustomFontFileDefinition(customFontFileValue);
							if (!fontFileDefinitions.ContainsKey(customFontFileDefinitionHash))
							{
								fontFileDefinitions[customFontFileDefinitionHash] = customFontFileValue;
								referencedCustomFontFileDefinitions.Add(customFontFileDefinitionHash);
							}

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
							// 	"font^[$POSIX]": "resource/scheme/fonts/linux/Renogare Linux.otf",
							// 	"name": "Renogare Soft"
							// }

							foreach (KeyValuePair<string, dynamic> property in customFontFileValue)
							{
								if (property.Key.Contains("font"))
								{
									files.Add(property.Value);
								}
							}
						}
					}
					else if (customFontFile.Value.GetType() == typeof(string))
					{
						// 'Guess' whether the font is used by checking if the file path contains the name of the font
						if (customFontFile.Value.Contains(fontName))
						{
							string customFontFileDefinitionHash = HashCustomFontFileDefinition(new Dictionary<string, dynamic>()
							{
								["font"] = customFontFile.Value
							});

							if (!fontFileDefinitions.ContainsKey(customFontFileDefinitionHash))
							{
								fontFileDefinitions[customFontFileDefinitionHash] = customFontFile.Value;
								referencedCustomFontFileDefinitions.Add(customFontFileDefinitionHash);
								files.Add(customFontFile.Value);
							}
						}
					}
				}
			}

			// Assign new indexes to the referenced custom font file definitions

			int highestKeyNumber = ((Dictionary<string, dynamic>)thisCustomFontFiles).Keys.ToArray().Aggregate<string, int>(0, (int a, string b) => Math.Max(a, int.Parse(b)));
			int customFontFilesKeysCount = thisCustomFontFiles.Keys.Count;

			int customFontFilesIndex = Math.Max(highestKeyNumber, customFontFilesKeysCount) + 1;

			foreach (string referencedCustomFontFileDefinition in referencedCustomFontFileDefinitions)
			{
				dynamic customFontFileDefinition = fontFileDefinitions[referencedCustomFontFileDefinition];
				newClientscheme["CustomFontFiles"][$"{customFontFilesIndex}"] = customFontFileDefinition;
				customFontFilesIndex++;
			}

			return newClientscheme;
		}

		private void WriteHUDLayout(string originFolderPath, HashSet<string> hudLayoutEntries, ClientschemeDependencies dependencies, FilesHashSet files)
		{
			string originHUDLayoutPath = $"{originFolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> originHUDLayout = new();

			void AddControls(string filePath, bool @base)
			{
				Dictionary<string, dynamic> obj = File.Exists(filePath) ? Utilities.VDFTryParse(filePath) : new()
				{
					["Resource/HudLayout.res"] = new Dictionary<string, dynamic>()
				};

				// #base
				if (obj.ContainsKey("#base"))
				{
					List<string> baseFiles = new();
					if (obj["#base"].GetType() == typeof(List<dynamic>))
					{
						foreach (dynamic baseFile in obj["#base"])
						{
							baseFiles.Add(baseFile);
						}
					}
					else
					{
						baseFiles.Add(obj["#base"]);
					}

					string[] folders = filePath.Split("\\");
					// Remove File Name
					folders[^1] = "";
					foreach (string baseFile in baseFiles)
					{
						AddControls(String.Join('\\', folders) + baseFile, true);
					}
				}

				// Merge
				foreach (string containerKey in obj.Keys)
				{
					if (obj[containerKey].GetType() == typeof(List<dynamic>))
					{
						foreach (dynamic item in obj[containerKey])
						{
							if (item.GetType() == typeof(Dictionary<string, dynamic>))
							{
								foreach (string hudLayoutEntryKey in item.Keys)
								{
									if (originHUDLayout.ContainsKey(hudLayoutEntryKey))
									{
										if (!@base)
										{
											originHUDLayout[hudLayoutEntryKey] = item[hudLayoutEntryKey];
										}
									}
									else
									{
										originHUDLayout[hudLayoutEntryKey] = item[hudLayoutEntryKey];
									}
								}
							}
							else
							{
								// There shouldn't be a top layer string hudlayout.res
							}
						}
					}
					else if (obj[containerKey].GetType() == typeof(Dictionary<string, dynamic>))
					{
						foreach (string hudLayoutEntryKey in obj[containerKey].Keys)
						{
							if (originHUDLayout.ContainsKey(hudLayoutEntryKey))
							{
								if (!@base)
								{
									originHUDLayout[hudLayoutEntryKey] = obj[containerKey][hudLayoutEntryKey];
								}
							}
							else
							{
								originHUDLayout[hudLayoutEntryKey] = obj[containerKey][hudLayoutEntryKey];
							}
						}
					}
					else
					{
						// There shouldn't be a top layer string that is not #base in hudlayout.res
					}
				}
			}

			AddControls(originHUDLayoutPath, false);

			string thisHUDLayoutPath = $"{this.FolderPath}\\scripts\\hudlayout.res";
			Dictionary<string, dynamic> newHUDLayout = Utilities.VDFTryParse(File.Exists(thisHUDLayoutPath) ? thisHUDLayoutPath : "Resources\\HUD\\scripts\\hudlayout.res");

			if (!newHUDLayout.ContainsKey("Resource/HudLayout.res"))
			{
				newHUDLayout["Resource/HudLayout.res"] = new Dictionary<string, dynamic>();
			}

			foreach (string hudLayoutEntry in hudLayoutEntries)
			{
				if (originHUDLayout.ContainsKey(hudLayoutEntry))
				{
					newHUDLayout["Resource/HudLayout.res"][hudLayoutEntry] = originHUDLayout[hudLayoutEntry];
					dependencies.Add("scripts", originHUDLayout[hudLayoutEntry], files);
				}
			}

			Directory.CreateDirectory($"{this.FolderPath}\\scripts");
			File.WriteAllText($"{this.FolderPath}\\scripts\\hudlayout.res", VDF.Stringify(newHUDLayout));

			files.Remove("scripts\\hudlayout.res");
		}

		/// <summary>
		/// Copies HUD Animations from origin HUD, adds clientscheme variables to provided Dependencies object
		/// </summary>
		private void WriteHUDAnimations(string originFolderPath, HashSet<string> events, string hudName, ClientschemeDependencies dependencies, FilesHashSet files)
		{
			if (events.Count == 0)
			{
				return;
			}

			string originHUDAnimationsManifestPath = $"{originFolderPath}\\scripts\\hudanimations_manifest.txt";

			Dictionary<string, dynamic> manifest = Utilities.VDFTryParse(Utilities.TestPath(originHUDAnimationsManifestPath) ? originHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
			List<string> hudAnimationsManifestList = new();
			foreach (dynamic filePath in manifest["hudanimations_manifest"]["file"])
			{
				hudAnimationsManifestList.Add(filePath);
			}

			string hudAnimationsPath = $"{this.FolderPath}\\scripts\\hudanimations_{hudName}.txt";

			Dictionary<string, List<HUDAnimation>> newHUDAnimations = File.Exists(hudAnimationsPath) ? Utilities.HUDAnimationsTryParse(hudAnimationsPath) : new();

			foreach (string filePath in hudAnimationsManifestList)
			{
				if (File.Exists($"{originFolderPath}\\{filePath}"))
				{
					Dictionary<string, List<HUDAnimation>> animationsFile = Utilities.HUDAnimationsTryParse($"{originFolderPath}\\{filePath}");

					// Add clientscheme colours and sounds referenced in HUD animations
					void AddEventDependencies(string @event, List<HUDAnimation> animationEvent)
					{
						newHUDAnimations[@event] = animationEvent;

						foreach (dynamic statement in animationEvent)
						{
							Type T = statement.GetType();

							if (T == typeof(Animate))
							{
								if (statement.Property.ToLower().Contains("color"))
								{
									dependencies.Colours.Add(statement.Value);
								}
							}

							if (T == typeof(RunEvent) || T == typeof(StopEvent) || T == typeof(RunEventChild))
							{
								// If the event called has not been evaluated, evaluate in
								// case there are more dependencies that need to be added
								if (!newHUDAnimations.ContainsKey(statement.Event) && animationsFile.ContainsKey(@event))
								{
									AddEventDependencies(statement.Event, animationsFile[@event]);
								}
							}

							if (T == typeof(PlaySound))
							{
								files.Add($"sound\\{statement.Sound}");
							}
						}
					}

					foreach (string @event in animationsFile.Keys)
					{
						if (events.Contains(@event))
						{
							AddEventDependencies(@event, animationsFile[@event]);
						}
					}
				}

			}

			Directory.CreateDirectory(Path.GetDirectoryName(hudAnimationsPath));
			File.WriteAllText(hudAnimationsPath, HUDAnimations.Stringify(newHUDAnimations));

			// Include origin animations file (Fake stringify VDF because we want to put the new animations file at the start to overwrite default tf2 animations)
			List<string> newManifestLines = new()
			{
				$"hudanimations_manifest",
				$"{{",
			};

			if (!hudAnimationsManifestList.Contains($"scripts/hudanimations_{hudName}.txt"))
			{
				newManifestLines.Add($"\t\"file\"\t\t\"scripts/hudanimations_{hudName}.txt\"");
			}

			string targetHUDAnimationsManifestPath = $"{this.FolderPath}\\scripts\\hudanimations_manifest.txt";

			Dictionary<string, dynamic> targetManifest = Utilities.VDFTryParse(File.Exists(targetHUDAnimationsManifestPath) ? targetHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
			dynamic targetHUDAnimationsManifestList = targetManifest["hudanimations_manifest"]["file"];

			foreach (string filePath in targetHUDAnimationsManifestList)
			{
				newManifestLines.Add($"\t\"file\"\t\t\"{filePath}\"");
			}

			newManifestLines.Add($"}}");

			File.WriteAllLines($"{this.FolderPath}\\scripts\\hudanimations_manifest.txt", newManifestLines);
		}

		/// <summary>
		/// Applies NewClientscheme to this HUD using #base
		/// </summary>
		private void WriteClientscheme(string originName, Dictionary<string, dynamic> newClientscheme)
		{
			bool writeBaseStatement = true;
			if (Utilities.TestPath($"{this.FolderPath}\\resource\\clientscheme.res"))
			{
				string[] lines = File.ReadAllLines($"{this.FolderPath}\\resource\\clientscheme.res");
				int i = 0;
				while (writeBaseStatement && i < lines.Length)
				{
					if (lines[i].Contains($"clientscheme_{originName}.res"))
					{
						writeBaseStatement = false;
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

			if (writeBaseStatement)
			{
				File.AppendAllLines($"{this.FolderPath}\\resource\\clientscheme.res", new string[]
				{
					"",
					$"\"#base\"\t\t\"clientscheme_{originName}.res\""
				});
			}

			// ALWAYS create new clientscheme.res

			// Remove empty clientscheme sections
			foreach (string key in newClientscheme.Keys)
			{
				if (newClientscheme[key].Keys.Count == 0)
				{
					newClientscheme.Remove(key);
				}
			}
			Dictionary<string, dynamic> newClientschemeContainer = new();
			newClientschemeContainer["Scheme"] = newClientscheme;
			Directory.CreateDirectory($"{this.FolderPath}\\resource");

			string clientschemeDependenciesPath = $"{this.FolderPath}\\resource\\clientscheme_{originName}.res";

			if (Utilities.TestPath(clientschemeDependenciesPath))
			{
				Dictionary<string, dynamic> previouslyMergedClientscheme = Utilities.VDFTryParse(clientschemeDependenciesPath);
				Utilities.Merge(previouslyMergedClientscheme, newClientschemeContainer);
				File.WriteAllText(clientschemeDependenciesPath, VDF.Stringify(previouslyMergedClientscheme));
			}
			else
			{
				File.WriteAllText(clientschemeDependenciesPath, VDF.Stringify(newClientschemeContainer));
			}
		}

		/// <summary>
		/// Copies list of HUD Files with no processing
		/// </summary>
		private void CopyHUDFiles(string originFolderPath, FilesHashSet files, ClientschemeDependencies dependencies)
		{
			foreach (string imagePath in dependencies.Images)
			{
				string[] folders = Regex.Split(imagePath, "[\\/]+");

				files.Add($"materials\\vgui\\{String.Join("\\", folders)}.vmt");
				files.Add($"materials\\vgui\\{String.Join("\\", folders)}.vtf");

				string filePath = $"{originFolderPath}\\materials\\vgui\\{String.Join("\\", folders)}.vmt";
				if (File.Exists(filePath))
				{
					Dictionary<string, dynamic> vmt = Utilities.VDFTryParse(filePath, false);
					Dictionary<string, dynamic> generic = vmt.First().Value;

					string vtfPath = "";
					int i = 0;

					while (vtfPath == "" && i < generic.Keys.Count)
					{
						if (generic.ElementAt(i).Key.ToLower().Contains("basetexture"))
						{
							vtfPath = generic.ElementAt(i).Value;
						}
						i++;
					}

					files.Add($"materials\\{vtfPath}");
				}
			}

			foreach (string audioPath in dependencies.Audio)
			{
				files.Add($"sound\\{audioPath}");
			}

			string[] filesArray = files.ToArray();
			foreach (string filePath in filesArray)
			{
				string sourceFileName = $"{originFolderPath}\\{filePath}";
				if (Utilities.TestPath(sourceFileName))
				{
					string destFileName = $"{this.FolderPath}\\{filePath}";
					Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
					File.Copy(sourceFileName, destFileName, true);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Could not find {sourceFileName}");
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
				Dictionary<string, dynamic> infoVDF = new();
				infoVDF[this.Name] = new Dictionary<string, dynamic>();
				infoVDF[this.Name]["ui_version"] = 3;
				File.WriteAllText($"{this.FolderPath}\\info.vdf", VDF.Stringify(infoVDF));
			}
		}
	}
}