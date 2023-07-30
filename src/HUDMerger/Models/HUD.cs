using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMergerVDF;
using HUDMergerVDF.Models;

namespace HUDMerger.Models;

/// <summary>
/// Represents a custom HUD
/// </summary>
public class HUD
{
	/// <summary>
	/// HUD Name (name of HUD folder)
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Absolute path to HUD Folder
	/// </summary>
	public string FolderPath { get; }

	/// <summary>
	/// Create HUD
	/// </summary>
	/// <param name="folderPath">Absolute path to HUD folder</param>
	public HUD(string folderPath)
	{
		Name = new DirectoryInfo(folderPath).Name;
		FolderPath = folderPath;
	}

	/// <summary>
	/// Returns whether the provided HUDPanel is 'in' this HUD
	/// </summary>
	/// <param name="panel">Panel</param>
	/// <returns></returns>
	public bool TestPanel(HUDPanel panel)
	{
		if (panel.RequiredKeyValue != null)
		{
			try
			{
				Dictionary<string, dynamic> obj = VDF.Parse(File.ReadAllText(Path.Join(FolderPath, panel.RequiredKeyValue.FilePath)));
				Dictionary<string, dynamic> objectRef = obj;
				string[] objectPath = panel.RequiredKeyValue.KeyPath.Split('.');
				foreach (string elementName in objectPath)
				{
					if (objectRef.ContainsKey(elementName))
					{
						objectRef = objectRef[elementName];
					}
					else
					{
						return false;
					}
				}
				return true;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
				return false;
			}
		}
		return Utilities.TestPath($"{FolderPath}\\{panel.Main.FilePath}");
	}

	/// <summary>
	/// Merges an array of HUDPanels from another HUD into this HUD
	/// </summary>
	/// <param name="origin">HUD to merge panels from</param>
	/// <param name="panels">Panels</param>
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

		(FilesHashSet files, HashSet<string> hudLayoutEntries, SchemeDependenciesManager dependencies, HashSet<string> events) = HUD.DestructurePanels(panels);
		dependencies.ClientScheme.Add(origin.FolderPath, files);
		WriteHUDLayout(origin.FolderPath, hudLayoutEntries, dependencies.ClientScheme, files);
		WriteHUDAnimations(origin.FolderPath, events, origin.Name, dependencies.ClientScheme, files);
		WriteScheme("client", GetDependencyValues(origin.FolderPath, "resource\\clientscheme.res", dependencies.ClientScheme, files));
		WriteScheme("source", GetDependencyValues(origin.FolderPath, "resource\\sourcescheme.res", dependencies.SourceScheme, files));
		CopyHUDFiles(origin.FolderPath, files, dependencies.ClientScheme);
		WriteInfoVDF();
	}

	/// <summary>
	/// Returns a list of Files and HUD Layout entries that should be used for the merge
	/// </summary>
	private static (FilesHashSet files, HashSet<string> hudLayoutEntries, SchemeDependenciesManager schemeDependencies, HashSet<string> events) DestructurePanels(HUDPanel[] panels)
	{
		FilesHashSet files = new FilesHashSet();
		HashSet<string> hudLayoutEntries = new();
		SchemeDependenciesManager dependencies = new();
		HashSet<string> events = new();
		foreach (HUDPanel panel in panels)
		{
			if (panel.Main?.FilePath != null)
			{
				files.Add(panel.Main.FilePath);
			}

			if (panel.Main?.HUDLayout != null)
			{
				foreach (string hudLayoutEntry in panel.Main.HUDLayout)
				{
					hudLayoutEntries.Add(hudLayoutEntry);
				}
			}

			if (panel.Main?.Events != null)
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

			// Client Scheme
			if (panel.Scheme?.ClientScheme?.Colours != null) dependencies.ClientScheme.Fonts.UnionWith(panel.Scheme.ClientScheme.Colours);
			if (panel.Scheme?.ClientScheme?.Borders != null) dependencies.ClientScheme.Fonts.UnionWith(panel.Scheme.ClientScheme.Borders);
			if (panel.Scheme?.ClientScheme?.Fonts != null) dependencies.ClientScheme.Fonts.UnionWith(panel.Scheme.ClientScheme.Fonts);

			// Source Scheme
			if (panel.Scheme?.SourceScheme?.Colours != null) dependencies.SourceScheme.Fonts.UnionWith(panel.Scheme.SourceScheme.Colours);
			if (panel.Scheme?.SourceScheme?.Borders != null) dependencies.SourceScheme.Fonts.UnionWith(panel.Scheme.SourceScheme.Borders);
			if (panel.Scheme?.SourceScheme?.Fonts != null) dependencies.SourceScheme.Fonts.UnionWith(panel.Scheme.SourceScheme.Fonts);
		}
		return (files, hudLayoutEntries, dependencies, events);
	}

	/// <summary>
	/// Returns the clientscheme values from a provided set of ClientschemeDependencies
	/// </summary>
	/// <param name="sourceHUDRoot">Absolute path to HUD folder</param>
	/// <param name="relativeSchemePath">Relative path to scheme file</param>
	/// <param name="dependencies">ClientschemeDependencies to get values of</param>
	/// <param name="files">File list for referenced asset files (such as .ttf files)</param>
	/// <returns></returns>
	private Dictionary<string, dynamic> GetDependencyValues(string sourceHUDRoot, string relativeSchemePath, ClientschemeDependencies dependencies, FilesHashSet files)
	{
		Dictionary<string, dynamic> originClientscheme = Utilities.LoadControls(sourceHUDRoot, relativeSchemePath);
		originClientscheme.TryAdd("Scheme", new Dictionary<string, dynamic>());

		Dictionary<string, dynamic> newClientscheme = new Dictionary<string, dynamic>
		{
			["Colors"] = new Dictionary<string, dynamic>(),
			["Borders"] = new Dictionary<string, dynamic>(),
			["Fonts"] = new Dictionary<string, dynamic>(),
			["CustomFontFiles"] = new Dictionary<string, dynamic>()
		};

		// Borders

		// Borders are first because a Border can reference a colour
		Dictionary<string, dynamic> borders = originClientscheme["Scheme"].ContainsKey("Borders") ? originClientscheme["Scheme"]["Borders"] : new Dictionary<string, dynamic>();
		foreach (string borderProperty in dependencies.Borders.Where(borders.ContainsKey))
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
					dependencies.Images.Add(Path.Join("materials\\vgui", property.Value));
				}
			}
		}

		// Colours
		Dictionary<string, dynamic> colours = originClientscheme["Scheme"].ContainsKey("Colors") ? originClientscheme["Scheme"]["Colors"] : new Dictionary<string, dynamic>();
		foreach (string colourProperty in dependencies.Colours)
		{
			if (colours.ContainsKey(colourProperty))
			{
				newClientscheme["Colors"][colourProperty] = colours[colourProperty];
			}
		}

		// Fonts
		Dictionary<string, dynamic> orginHUDFonts = originClientscheme["Scheme"].ContainsKey("Fonts") ? originClientscheme["Scheme"]["Fonts"] : new Dictionary<string, dynamic>();

		HashSet<string> fontNames = new(); // StringComparer.OrdinalIgnoreCase

		foreach (string fontDefinitionName in dependencies.Fonts.Where(fontDefinitionName => orginHUDFonts.ContainsKey(fontDefinitionName)))
		{
			newClientscheme["Fonts"][fontDefinitionName] = orginHUDFonts[fontDefinitionName];
			foreach (KeyValuePair<string, dynamic> i in orginHUDFonts[fontDefinitionName])
			{
				if (i.Value is Dictionary<string, dynamic> keyValues)
				{
					fontNames.UnionWith(keyValues.Where((kv) => kv.Key.Contains("name")).Select(kv => $"{kv.Value}"));
				}
			}
		}

		// Add custom fonts
		Dictionary<string, dynamic> originCustomFontFileDefinitions = originClientscheme["Scheme"].ContainsKey("CustomFontFiles") ? originClientscheme["Scheme"]["CustomFontFiles"] : new Dictionary<string, dynamic>();

		// Intermediate set of referenced custom font file definitions to go into this hud. These definitions will be copied when we know the new numbers for keys
		HashSet<dynamic> referencedCustomFontFileDefinitions = new();

		// Highest Key Number
		int highestKeyNumber = ((Dictionary<string, dynamic>)Utilities.LoadControls(File.Exists(
			Path.Join(FolderPath, "resource\\clientscheme.res"))
			? FolderPath
			: "Resources\\HUD",
			"resource\\clientscheme.res")["Scheme"]["CustomFontFiles"])
			.Keys.Aggregate<string, int>(0, (int a, string b) => Math.Max(a, int.Parse(b)));

		foreach (KeyValuePair<string, dynamic> customFont in originCustomFontFileDefinitions)
		{
			if (customFont.Value is Dictionary<string, dynamic> customFontDefinition)
			{
				IEnumerable<string> customFontFileDefinitionfontNames = customFontDefinition.Where(kv => kv.Key.StartsWith("name")).Select(kv => $"{kv.Value}");

				if (customFontFileDefinitionfontNames.Any(fontNames.Contains))
				{
					referencedCustomFontFileDefinitions.Add(customFontDefinition);
					files.UnionWith(customFontDefinition.Where(kv => kv.Key.StartsWith("font")).Select(kv => $"{kv.Value}"));
					highestKeyNumber = Math.Max(highestKeyNumber, int.Parse(customFont.Key));
				}
			}
			else if (customFont.Value.GetType() == typeof(string))
			{
				// 'Guess' whether the font is used by checking if the file path contains the name of the font
				string customFontValue = customFont.Value.ToLower();
				if (fontNames.Any(fontName => customFontValue.Contains(fontName.ToLower())))
				{
					referencedCustomFontFileDefinitions.Add(customFont.Value);
					files.Add(customFont.Value);
					highestKeyNumber = Math.Max(highestKeyNumber, int.Parse(customFont.Key));
				}
			}
		}

		// Assign new indexes to the referenced custom font file definitions
		foreach (dynamic customFontFileDefinition in referencedCustomFontFileDefinitions)
		{
			// Create new key number first, or we will overwrite previous highest key
			highestKeyNumber++;
			newClientscheme["CustomFontFiles"][$"{highestKeyNumber}"] = customFontFileDefinition;
		}

		return newClientscheme;
	}

	private void WriteHUDLayout(string originFolderPath, HashSet<string> hudLayoutEntries, ClientschemeDependencies dependencies, FilesHashSet files)
	{
		if (!hudLayoutEntries.Any()) return;

		Dictionary<string, dynamic> originHUDLayout = Utilities.LoadControls(originFolderPath, "scripts\\hudlayout.res").First(kv => kv.Value.GetType() == typeof(Dictionary<string, dynamic>)).Value;

		string thisHUDLayoutPath = Path.Join(FolderPath, "scripts\\hudlayout.res");
		Dictionary<string, dynamic> newHUDLayout = Utilities.LoadControls(File.Exists(thisHUDLayoutPath) ? FolderPath : "Resources\\HUD", "scripts\\hudlayout.res");

		newHUDLayout.TryAdd("Resource/HudLayout.res", new Dictionary<string, dynamic>());

		foreach (string hudLayoutEntry in hudLayoutEntries.Where(originHUDLayout.ContainsKey))
		{
			newHUDLayout["Resource/HudLayout.res"][hudLayoutEntry] = originHUDLayout[hudLayoutEntry];

			// Keep zpos from source HUD to avoid layer conflicts
			foreach (KeyValuePair<string, dynamic> zpos in ((Dictionary<string, dynamic>)originHUDLayout[hudLayoutEntry]).Where(kv => kv.Key.StartsWith("zpos")))
			{
				newHUDLayout["Resource/HudLayout.res"][hudLayoutEntry][zpos.Key] = zpos.Value;
			}

			// Add HUD Layout entries referenced with pin_to_sibling (primarily used for forcing engineer building status position)
			foreach (KeyValuePair<string, dynamic> pin_to_sibling in ((Dictionary<string, dynamic>)originHUDLayout[hudLayoutEntry]).Where(kv => kv.Key.ToLower().StartsWith("pin_to_sibling") && originHUDLayout.ContainsKey(kv.Value)))
			{
				newHUDLayout["Resource/HudLayout.res"][pin_to_sibling.Value] = originHUDLayout[pin_to_sibling.Value];
			}

			dependencies.Add(originHUDLayout[hudLayoutEntry]);
		}

		Directory.CreateDirectory(Path.Join(FolderPath, "scripts"));
		File.WriteAllText(Path.Join(FolderPath, "scripts\\hudlayout.res"), VDF.Stringify(newHUDLayout));

#if DEBUG
		if (files.Contains("scripts\\hudlayout.res"))
		{
			throw new Exception();
		}
#endif

		files.Remove("scripts\\hudlayout.res");
	}

	/// <summary>
	/// Write Scheme
	/// </summary>
	/// <param name="schemeType">Scheme type</param>
	/// <param name="newScheme">Scheme contents</param>
	private void WriteScheme(string schemeType, Dictionary<string, dynamic> newScheme)
	{
		if (newScheme.All(kv => !((Dictionary<string, dynamic>)kv.Value).Any())) return;

		Dictionary<string, dynamic> newSchemeContainer = new();
		newSchemeContainer["Scheme"] = newScheme;

		string schemePath = Path.Join(FolderPath, $"resource\\{schemeType}scheme.res");

		if (Utilities.TestPath(schemePath))
		{
			Dictionary<string, dynamic> previouslyMergedScheme = Utilities.VDFTryParse(schemePath);
			Utilities.OverWriteSchemeEntries(previouslyMergedScheme, newSchemeContainer);
			File.WriteAllText(schemePath, VDF.Stringify(previouslyMergedScheme));
		}
		else
		{
			Utilities.CopyResourceToHUD($"resource\\{schemeType}scheme.res", FolderPath);
			File.WriteAllText(schemePath, VDF.Stringify(newSchemeContainer));
		}
	}

	/// <summary>
	/// Copies HUD Animations from origin HUD, adds clientscheme variables to provided Dependencies object
	/// </summary>
	private void WriteHUDAnimations(string originFolderPath, HashSet<string> events, string hudName, ClientschemeDependencies dependencies, FilesHashSet files)
	{
		if (!events.Any()) return;

		string originHUDAnimationsManifestPath = $"{originFolderPath}\\scripts\\hudanimations_manifest.txt";

		Dictionary<string, dynamic> manifest = Utilities.VDFTryParse(Utilities.TestPath(originHUDAnimationsManifestPath)
			? originHUDAnimationsManifestPath
			: "Resources\\HUD\\scripts\\hudanimations_manifest.txt");

		List<string> hudAnimationsManifestList = new();
		foreach (dynamic filePath in manifest["hudanimations_manifest"]["file"])
		{
			hudAnimationsManifestList.Add(filePath);
		}

		string hudAnimationsPath = $"{FolderPath}\\scripts\\hudanimations_{hudName}.txt";

		Dictionary<string, List<HUDAnimation>> newHUDAnimations = File.Exists(hudAnimationsPath) ? Utilities.HUDAnimationsTryParse(hudAnimationsPath) : new();

		foreach (string filePath in hudAnimationsManifestList)
		{
			if (File.Exists($"{originFolderPath}\\{filePath}"))
			{
				Dictionary<string, List<HUDAnimation>> animationsFile = Utilities.HUDAnimationsTryParse($"{originFolderPath}\\{filePath}");

				// Add clientscheme colours and sounds referenced in HUD animations
				void AddEventDependencies(string @event, List<HUDAnimation> animationEvent)
				{
					// Add first occurrence of event
					if (!newHUDAnimations.ContainsKey(@event))
					{
						newHUDAnimations[@event] = animationEvent;
					}

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

		string targetHUDAnimationsManifestPath = $"{FolderPath}\\scripts\\hudanimations_manifest.txt";

		Dictionary<string, dynamic> targetManifest = Utilities.VDFTryParse(File.Exists(targetHUDAnimationsManifestPath) ? targetHUDAnimationsManifestPath : "Resources\\HUD\\scripts\\hudanimations_manifest.txt");
		dynamic targetHUDAnimationsManifestList = targetManifest["hudanimations_manifest"]["file"];

		foreach (string filePath in targetHUDAnimationsManifestList)
		{
			newManifestLines.Add($"\t\"file\"\t\t\"{filePath}\"");
		}

		newManifestLines.Add($"}}");

		File.WriteAllLines($"{FolderPath}\\scripts\\hudanimations_manifest.txt", newManifestLines);
	}

	/// <summary>
	/// Copies list of HUD Files with no processing
	/// </summary>
	private void CopyHUDFiles(string originFolderPath, FilesHashSet files, ClientschemeDependencies dependencies)
	{
		foreach (string imagePath in dependencies.Images)
		{
			files.Add($"{imagePath}.vmt");
			files.Add($"{imagePath}.vtf");

			string vmtPath = Path.Join(originFolderPath, $"{imagePath}.vmt");

			if (File.Exists(vmtPath))
			{
				Dictionary<string, dynamic> vmt = Utilities.VDFTryParse(vmtPath, new VDFParseOptions { OSTags = VDFOSTags.None });
				Dictionary<string, dynamic> generic = vmt.First().Value;
				string vtfPath = $"{generic.FirstOrDefault(kv => kv.Key.ToLower().Contains("basetexture")).Value}";
				if (!String.IsNullOrEmpty(vtfPath))
				{
					files.Add(Path.Join("materials", $"{vtfPath}"));
					files.Add(Path.Join("materials", $"{vtfPath}.vtf"));
				}
			}
		}

		foreach (string audioPath in dependencies.Audio)
		{
			files.Add(audioPath);
		}

		string[] filesArray = files.ToArray();
		foreach (string filePath in filesArray)
		{
			string sourceFileName = Path.Join(originFolderPath, filePath);
			string destFileName = Path.Join(FolderPath, filePath);
			if (Utilities.TestPath(sourceFileName))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
				File.Copy(sourceFileName, destFileName, true);
			}
			else
			{
				if (Utilities.TestPath(destFileName))
				{
					// Delete file to avoid conflicts
					File.Delete(destFileName);
				}
			}
		}
	}

	/// <summary>
	/// Writes an info.vdf file to the current HUD if it doesn't exist
	/// </summary>
	private void WriteInfoVDF()
	{
		// UI Version
		string infoVDFPath = Path.Join(FolderPath, "info.vdf");
		if (!File.Exists(infoVDFPath))
		{
			Dictionary<string, dynamic> infoVDF = new();
			infoVDF[Name] = new Dictionary<string, dynamic>();
			infoVDF[Name]["ui_version"] = 3;
			File.WriteAllText(infoVDFPath, VDF.Stringify(infoVDF));
		}
	}
}
