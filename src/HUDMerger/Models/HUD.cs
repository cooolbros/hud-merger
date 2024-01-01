using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HUDAnimations.Models;
using HUDAnimations.Models.Animations;
using HUDMerger.Exceptions;
using HUDMerger.Extensions;
using HUDMerger.Models.Scheme;
using HUDMerger.Services;
using VDF;
using VDF.Models;

namespace HUDMerger.Models;

/// <summary>
/// Represents a custom HUD
/// </summary>
/// <remarks>
/// Create HUD
/// </remarks>
/// <param name="folderPath">Absolute path to HUD folder</param>
public class HUD(string folderPath)
{
	/// <summary>
	/// HUD Name (name of HUD folder)
	/// </summary>
	public string Name { get; } = new DirectoryInfo(folderPath).Name;

	/// <summary>
	/// Absolute path to HUD Folder
	/// </summary>
	public string FolderPath { get; } = folderPath;

	public HUDPanel[] Panels { get; } = JsonSerializer
		.Deserialize<HUDPanel[]>(File.OpenRead("Resources\\Panels.json"))!
		.Where((panel) =>
		{
			if (panel.RequiredKeyValue != null)
			{
				try
				{
					FilesHashSet seen = [];

					bool TestFile(FileInfo file)
					{
						if (seen.Contains(file.FullName))
						{
							return false;
						}

						seen.Add(file.FullName);

						if (!file.Exists)
						{
							return false;
						}

						KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(file.FullName));

						bool result = TestKeyValues(keyValues);
						if (result)
						{
							return result;
						}

						foreach (string baseFile in keyValues.BaseFiles())
						{
							result = TestFile(new FileInfo(Path.Join(file.DirectoryName, baseFile)));
							if (result)
							{
								return result;
							}
						}

						return false;
					}

					bool TestKeyValues(KeyValues keyValues)
					{
						KeyValues obj = keyValues.Header();

						foreach (string key in panel.RequiredKeyValue.KeyPath)
						{
							dynamic value = obj.FirstOrDefault((keyValue) => keyValue.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
							if (value != null)
							{
								obj = value;
							}
							else
							{
								return false;
							}
						}

						return true;
					}

					return TestFile(new FileInfo(Path.Join(folderPath, panel.RequiredKeyValue.FilePath)));
				}
				catch
				{
					return false;
				}
			}
			return File.Exists(Path.Join(folderPath, panel.Main));
		})
		.ToArray();

	/// <summary>
	/// Merges an array of HUDPanels from another HUD into this HUD
	/// </summary>
	/// <param name="source">HUD to merge panels from</param>
	/// <param name="target">HUD to merge panels to</param>
	/// <param name="panels">Panels</param>
	public static void Merge(HUD source, HUD target, HUDPanel[] panels)
	{
		Dependencies dependencies = new(panels.Select((panel) => panel.Dependencies).OfType<Dependencies>());
		dependencies.Files.UnionWith(panels.Select((panel) => panel.Main).Where((main) => !string.IsNullOrEmpty(main)));

		HUDFileReaderService reader = new();

		Func<HUDFileReaderService, HUD, HUD, Dependencies, Action<HUDFileWriterService>?>[] actions =
		[
			AddDependencies,
			MergeHUDLayout,
			MergeHUDAnimations,
			MergeClientScheme,
			MergeSourceScheme,
			MergeLanguageTokens,
			MergeImages,
			MergeAudio,
			MergeInfoVDF,
			CopyFiles
		];

		List<Action<HUDFileWriterService>> commitActions = [];
		List<Exception> exceptions = [];

		foreach (Func<HUDFileReaderService, HUD, HUD, Dependencies, Action<HUDFileWriterService>?> action in actions)
		{
			try
			{
				Action<HUDFileWriterService>? result = action(reader, source, target, dependencies);
				if (result != null)
				{
					commitActions.Add(result);
				}
			}
			catch (AggregateException e)
			{
				exceptions.AddRange(e.InnerExceptions);
			}
		}

		if (exceptions.Count != 0)
		{
			throw new Exception(string.Join("\r\n", [
				$"{exceptions.Count} error{(exceptions.Count != 1 ? "s" : "")} were encountered while trying to merge:",
				"",
				..exceptions
					.OrderBy((e) => e is FileException fileException ? (fileException.HUD == source ? 0 : 1) : 0)
					.Select((e) => $"• {e.Message}\r\n"),
				"",
				$"No changes have been applied to {target.Name}."
			]));
		}

		HUDFileWriterService writer = new(target.FolderPath);

		foreach (Action<HUDFileWriterService> action in commitActions)
		{
			action(writer);
		}
	}

	private static Action<HUDFileWriterService>? AddDependencies(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		dependencies.Add(reader, source);
		return null;
	}

	private static Action<HUDFileWriterService>? MergeHUDLayout(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		if (dependencies.HUDLayout.Count == 0)
		{
			return null;
		}

		reader.Require([
			(source, "scripts\\hudlayout.res", FileType.VDF),
			(target, "scripts\\hudlayout.res", FileType.VDF)
		]);

		HUDLayout sourceHUDLayout = new(reader, source);

		KeyValues targetHUDLayout = reader.ReadKeyValues(target, "scripts\\hudlayout.res");
		KeyValues targetHeader = targetHUDLayout.Header("Resource/HudLayout.res");

		foreach (string hudLayoutEntry in dependencies.HUDLayout)
		{
			List<KeyValue> targetEntries = targetHeader
				.Where((entry) => entry.Key.Equals(hudLayoutEntry, StringComparison.OrdinalIgnoreCase))
				.ToList();

			foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> entry in sourceHUDLayout[hudLayoutEntry])
			{
				KeyValue targetEntry = targetEntries.FirstOrDefault((kv) => KeyValueComparer.KeyComparer.Equals(kv, entry.Key));

				int entryIndex = targetHeader.IndexOf(targetEntry);

				if (entryIndex != -1)
				{
					targetEntries.Remove(targetEntry);
					int index = entryIndex;

					List<KeyValue?> sourceEntry = entry.Value.Select(KeyValue? (kv) => kv).ToList();

					// Preserve zpos from target HUD layout entry to avoid layer conflicts

					// Get zpos from entry.Value instead of sourceEntry because sourceEntry is nullable
					//
					// We can use Dictionary<KeyValue, int> instead of List<KeyValue> here because
					// new HUDLayout() already removed duplicate zpos values so ToDictionary will not throw
					Dictionary<KeyValue, int> zposIndices = entry.Value
						.Where((kv) => kv.Key.Equals("zpos", StringComparison.OrdinalIgnoreCase))
						.Select((entry) => KeyValuePair.Create(entry, sourceEntry.IndexOf(entry)))
						.ToDictionary(KeyValueComparer.KeyComparer);

					foreach (KeyValue zpos in ((KeyValues)targetHeader[index].Value).Where((kv) => kv.Key.Equals("zpos", StringComparison.OrdinalIgnoreCase)))
					{
						if (zposIndices.TryGetValue(zpos, out int zposIndex))
						{
							sourceEntry[zposIndex] = new KeyValue
							{
								Key = sourceEntry[zposIndex]!.Value.Key,
								Value = zpos.Value,
								Conditional = sourceEntry[zposIndex]!.Value.Conditional,
							};

							zposIndices.Remove(zpos);
						}
						else
						{
							sourceEntry.Add(zpos);
						}
					}

					// Remove all zpos values that have not already been replaced by the source
					foreach (int zposIndex in zposIndices.Values)
					{
						sourceEntry[zposIndex] = null;
					}

					List<KeyValue> entryResult = sourceEntry.OfType<KeyValue>().ToList();

					targetHeader[index] = new KeyValue
					{
						Key = entry.Key.Key,
						Value = entryResult,
						Conditional = entry.Key.Conditional
					};

					// Copy HUD layout entries that the current entry is pinned to
					IEnumerable<string> pin_to_siblings = entryResult
						.Where((kv) => kv.Key.Equals("pin_to_sibling", StringComparison.OrdinalIgnoreCase))
						.Select(kv => kv.Value)
						.OfType<string>();

					int pinIndex = index + 1;

					foreach (string pin_to_sibling in pin_to_siblings)
					{
						foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> pinEntry in sourceHUDLayout[pin_to_sibling])
						{
							KeyValue pinElement = new()
							{
								Key = pinEntry.Key.Key,
								Value = pinEntry.Value,
								Conditional = pinEntry.Key.Conditional
							};

							int pinElementIndex = targetHeader.FindIndex((e) => KeyValueComparer.KeyComparer.Equals(e, pinEntry.Key));

							if (pinElementIndex != -1)
							{
								targetHeader[pinElementIndex] = pinElement;
							}
							else
							{
								targetHeader.Insert(pinIndex++, pinElement);
							}
						}
					}
				}
				else
				{
					targetHeader.Add(new KeyValue
					{
						Key = entry.Key.Key,
						Value = entry.Value,
						Conditional = entry.Key.Conditional
					});
				}

				dependencies.Add(entry.Value);
			}

			// Remove all HUD layout entries that have not already been replaced by the source
			foreach (KeyValue e in targetEntries)
			{
				targetHeader.Remove(e);
			}
		}

		FilesHashSet seen = [];
		Dictionary<string, KeyValues> baseKeyValues = [];

		void RemoveBaseHUDLayoutEntries(string folderPath, IEnumerable<string> baseFiles)
		{
			foreach (string baseFile in baseFiles)
			{
				string relativePath = Path.GetRelativePath(".", Path.Join(folderPath, baseFile));
				KeyValues? keyValues = reader.TryReadKeyValues(target, relativePath);

				if (!seen.Add(relativePath) || keyValues == null) continue;

				KeyValues header = keyValues.Header();

				List<KeyValue> removeList = header
					.Where((entry) => dependencies.HUDLayout.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
					.ToList();

				foreach (KeyValue entry in removeList)
				{
					header.Remove(entry);
				}

				baseKeyValues[relativePath] = keyValues;

				string? directoryName = Path.GetDirectoryName(relativePath);
				if (directoryName != null)
				{
					RemoveBaseHUDLayoutEntries(directoryName, keyValues.BaseFiles());
				}
			}
		}

		RemoveBaseHUDLayoutEntries("scripts", targetHUDLayout.BaseFiles());

#if DEBUG
		if (dependencies.Files.Contains("scripts\\hudlayout.res"))
		{
			throw new UnreachableException("dependencies.Files Contains \"scripts\\hudlayout.res\"");
		}
#endif

		return (writer) =>
		{
			writer.Write("scripts\\hudlayout.res", targetHUDLayout);
			foreach (KeyValuePair<string, KeyValues> kvp in baseKeyValues)
			{
				writer.Write(kvp.Key, kvp.Value);
			}
		};
	}

	private static Action<HUDFileWriterService>? MergeHUDAnimations(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		if (dependencies.Events.Count == 0)
		{
			return null;
		}

		reader.Require([
			(source, "scripts\\hudanimations_manifest.txt", FileType.VDF),
			(target, "scripts\\hudanimations_manifest.txt", FileType.VDF),
			(target, $"scripts\\hudanimations_{source.Name}.txt", FileType.HUDAnimations)
		]);

		string[] hudAnimationsManifestList = reader
			.ReadKeyValues(source, "scripts\\hudanimations_manifest.txt")
			.Header("hudanimations_manifest")
			.Where((keyValue) => keyValue.Key.Equals("file", StringComparison.OrdinalIgnoreCase))
			.Select((keyValue) => keyValue.Value)
			.OfType<string>()
			.Select((file) => App.PathSeparatorRegex().Replace(file, "\\"))
			.ToArray();

		reader.Require(hudAnimationsManifestList.Select((file) => (source, file, FileType.HUDAnimations)));

		HUDAnimationsFile targetMergeHUDAnimations = reader.TryReadHUDAnimations(target, $"scripts\\hudanimations_{source.Name}.txt") ?? [];

		foreach (string filePath in hudAnimationsManifestList)
		{
			HUDAnimationsFile? animationsFile = reader.TryReadHUDAnimations(source, filePath);
			if (animationsFile == null) continue;

			void AddEventDependencies(KeyValue eventKeyValue)
			{
				if (targetMergeHUDAnimations.Contains(eventKeyValue))
				{
					return;
				}

				int index = targetMergeHUDAnimations.FindIndex((e) => KeyValueComparer.KeyComparer.Equals(e, eventKeyValue));
				if (index != -1)
				{
					targetMergeHUDAnimations[index] = eventKeyValue;
				}
				else
				{
					targetMergeHUDAnimations.Add(eventKeyValue);
				}

				if (eventKeyValue.Value is List<HUDAnimationBase> statements)
				{
					void Add(string eventName)
					{
						foreach (KeyValue kv in animationsFile.Where((eventKeyValue) => eventKeyValue.Key.Equals(eventName, StringComparison.OrdinalIgnoreCase)))
						{
							AddEventDependencies(kv);
						}
					}

					foreach (HUDAnimationBase statement in statements)
					{
						switch (statement)
						{
							case Animate animate when animate.Property.Contains("color", StringComparison.OrdinalIgnoreCase):
								dependencies.ClientScheme.Colours.Add(animate.Property);
								break;
							case RunEvent runEvent:
								Add(runEvent.Event);
								break;
							case StopEvent runEvent:
								Add(runEvent.Event);
								break;
							case RunEventChild runEvent:
								Add(runEvent.Event);
								break;
							case PlaySound playSound:
								dependencies.Audio.Add($"sound\\{playSound.Sound}");
								break;
						}
					}
				}
			}

			foreach (KeyValue eventKeyValue in animationsFile)
			{
				if (dependencies.Events.Contains(eventKeyValue.Key))
				{
					AddEventDependencies(eventKeyValue);
				}
			}
		}

		KeyValues targetHUDAnimationsManifest = reader.ReadKeyValues(target, "scripts\\hudanimations_manifest.txt");
		KeyValues targetHUDAnimationsManifestHeader = targetHUDAnimationsManifest.Header("hudanimations_manifest");

		bool SourceHUDAnimationsFileExists(KeyValue kv)
		{
			return kv.Key.Equals("file", StringComparison.OrdinalIgnoreCase)
				&& kv.Value is string str
				&& str.Equals($"scripts/hudanimations_{source.Name}.txt", StringComparison.OrdinalIgnoreCase);
		}

		bool writeTargetHUDAnimationsManifest = !targetHUDAnimationsManifestHeader.Any(SourceHUDAnimationsFileExists);

		if (writeTargetHUDAnimationsManifest)
		{
			targetHUDAnimationsManifestHeader.Insert(0, new KeyValue
			{
				Key = "file",
				Value = $"scripts/hudanimations_{source.Name}.txt",
				Conditional = null
			});
		}

		return (writer) =>
		{
			writer.Write($"scripts\\hudanimations_{source.Name}.txt", targetMergeHUDAnimations);
			if (writeTargetHUDAnimationsManifest)
			{
				writer.Write("scripts\\hudanimations_manifest.txt", targetHUDAnimationsManifest);
			}
		};
	}

	private static Action<HUDFileWriterService>? MergeClientScheme(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		if (!dependencies.ClientScheme.Any())
		{
			return null;
		}

		reader.Require([
			(source, "resource\\clientscheme.res", FileType.VDF),
			(target, $"resource\\clientscheme_{source.Name}.res", FileType.VDF),
			(target, "resource\\clientscheme.res", FileType.VDF)
		]);

		ClientScheme sourceClientScheme = new(reader, source, "resource\\clientscheme.res");

		string targetMergeSchemeRelativePath = $"resource\\clientscheme_{source.Name}.res";

		KeyValues? targetMergeClientSchemeKeyValues = reader.TryReadKeyValues(target, targetMergeSchemeRelativePath);

		ClientScheme targetMergeClientScheme = targetMergeClientSchemeKeyValues != null
			? new ClientScheme(reader, target, targetMergeSchemeRelativePath, targetMergeClientSchemeKeyValues)
			: new();

		void AddBorderDependencies(string borderName, bool directDependency)
		{
			IEnumerable<KeyValuePair<KeyValue, dynamic>> borders = sourceClientScheme.GetBorder(borderName);
			targetMergeClientScheme.SetBorder(borders.ToList());

			foreach (KeyValuePair<KeyValue, dynamic> border in borders)
			{
				switch (border.Value)
				{
					case IEnumerable<KeyValue> borderValue:
						foreach (KeyValue kv in borderValue)
						{
							if (kv.Key.Contains("color", StringComparison.OrdinalIgnoreCase) && kv.Value is string colour)
							{
								dependencies.ClientScheme.Colours.Add(colour);
							}

							if (kv.Key.Contains("image", StringComparison.OrdinalIgnoreCase) && kv.Value is string image)
							{
								dependencies.Images.Add($"materials\\vgui\\{image}");
							}
						}
						break;
					case string borderReference when directDependency: // border references are not recursive
						AddBorderDependencies(borderReference, false);
						break;
				}
			}
		}

		foreach (string borderName in dependencies.ClientScheme.Borders)
		{
			AddBorderDependencies(borderName, true);
		}

		foreach (string colourName in dependencies.ClientScheme.Colours)
		{
			targetMergeClientScheme.SetColour(sourceClientScheme.GetColour(colourName));
		}

		HashSet<string> fontNames = new(StringComparer.OrdinalIgnoreCase);

		foreach (string fontName in dependencies.ClientScheme.Fonts)
		{
			IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> fonts = sourceClientScheme.GetFont(fontName);
			targetMergeClientScheme.SetFont(fonts.ToList());

			fontNames.UnionWith(
				/*
					Select the "name" values from each font item entry

					"HudFontGiantBold"
					{
						"1"
						{
							"name"		"TF2 Build" // <--
							"tall"		"44"
							"tall_lodef"	"52"
							"weight"	"500"
							"additive"	"0"
							"antialias" "1"
						}
					}

					"HudFontBiggerBold"
					{
						"1"
						{
							"name"		"TF2 Build" // <--
							"tall"		"35"
							"tall_lodef"	"40"
							"weight"	"500"
							"additive"	"0"
							"antialias" "1"
						}
					}
				*/
				fonts
					.SelectMany((kvp) => kvp.Value)
					.Select((kv) => kv.Value)
					.OfType<IEnumerable<KeyValue>>()
					.SelectMany((kv) => kv)
					.Where(kv => kv.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
					.Select((kv) => kv.Value)
					.OfType<string>()
			);
		}

		KeyValues targetClientSchemeEntryKeyValues = reader.ReadKeyValues(target, "resource\\clientscheme.res");

		ClientScheme targetClientScheme = new(reader, target, "resource\\clientscheme.res", targetClientSchemeEntryKeyValues);

		int max = Math.Max(sourceClientScheme.CustomFontFiles.Count, targetClientScheme.CustomFontFiles.Count);

		HashSet<KeyValue> referencedCustomFontFiles = [];

		foreach (KeyValue customFontFile in sourceClientScheme.CustomFontFiles)
		{
			if (int.TryParse(customFontFile.Key, out int i))
			{
				max = Math.Max(max, i);
			}

			List<string> customFontFileFontNames = customFontFile.Value switch
			{
				/*
					Select the "name" values from each CustomFontFile entry
					If the value is a string select the string value

					"1" "resource/tf.ttf"
					"2" "resource/tfd.ttf"
					"3"
					{
						"font" "resource/TF2.ttf"
						"name" "TF2" // <--
						"russian"
						{
							"range" "0x0000 0xFFFF"
						}
						"polish"
						{
							"range" "0x0000 0xFFFF"
						}
					}
				*/
				KeyValues keyValues => keyValues
					.Where((kv) => kv.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
					.Select((kv) => kv.Value)
					.OfType<string>()
					.ToList(),
				string str => [str],
				_ => throw new UnreachableException()
			};

			// The CustomFontFile is referenced if any of it's font names are equal to any of the clientscheme Fonts names
			bool referenced = customFontFileFontNames.Any((customFontFileFontName) =>
				fontNames.Any((fontName) =>
					customFontFileFontName.Equals(fontName, StringComparison.OrdinalIgnoreCase)
				)
			);

			if (referenced)
			{
				referencedCustomFontFiles.Add(customFontFile);

				dependencies.Files.UnionWith(customFontFile.Value switch
				{
					KeyValues keyValues => keyValues
						.Where((kv) => kv.Key.Equals("font", StringComparison.OrdinalIgnoreCase))
						.Select((kv) => kv.Value)
						.OfType<string>(),
					string str => [str],
					_ => throw new UnreachableException()
				});
			}
		}

		foreach (KeyValue customFontFile in referencedCustomFontFiles)
		{
			// Equals ignore KeyValue.Key because it is replaced by the max number
			bool exists = targetMergeClientScheme.CustomFontFiles.Any((kv) =>
			{
				bool conditionalsEqual = (kv.Conditional != null && customFontFile.Conditional != null)
					? kv.Conditional.Equals(customFontFile.Conditional, StringComparison.OrdinalIgnoreCase)
					: kv.Conditional == customFontFile.Conditional;

				return kv.Value.Equals(customFontFile.Value) && conditionalsEqual;
			});

			if (!exists)
			{
				targetMergeClientScheme.CustomFontFiles.Add(new KeyValue
				{
					Key = max++.ToString(),
					Value = customFontFile.Value,
					Conditional = customFontFile.Conditional
				});
			}
		}

		bool writeClientScheme = !targetClientSchemeEntryKeyValues.BaseFiles().ToArray().Contains($"clientscheme_{source.Name}.res", StringComparer.OrdinalIgnoreCase);

		if (writeClientScheme)
		{
			int index = targetClientSchemeEntryKeyValues.FindLastIndex((kv) => kv.Key.Equals("#base", StringComparison.OrdinalIgnoreCase) && kv.Value is string);
			targetClientSchemeEntryKeyValues.Insert(
				index != -1 ? (index + 1) : 0,
				new KeyValue
				{
					Key = "#base",
					Value = $"clientscheme_{source.Name}.res",
					Conditional = null
				}
			);
		}

		return (writer) =>
		{
			writer.Write(targetMergeSchemeRelativePath, targetMergeClientScheme.ToKeyValues());
			if (writeClientScheme)
			{
				writer.Write("resource\\clientscheme.res", targetClientSchemeEntryKeyValues);
			}
		};
	}

	private static Action<HUDFileWriterService>? MergeSourceScheme(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		if (!dependencies.SourceScheme.Any())
		{
			return null;
		}

		reader.Require([
			(source, "resource\\sourcescheme.res", FileType.VDF),
			(target, "resource\\sourcescheme.res", FileType.VDF)
		]);

		SourceScheme sourceSourceScheme = new(reader, source, "resource\\sourcescheme.res");

		KeyValues targetSourceSchemeKeyValues = reader.ReadKeyValues(target, "resource\\sourcescheme.res");
		KeyValues targetSourceSchemeHeader = targetSourceSchemeKeyValues.Header("Scheme");
		KeyValues targetSourceSchemeFonts;

		// TF2 only loads the first "Fonts" section in the sourcescheme entry file
		// Assume the first "Fonts" section does not have a conditional
		switch (targetSourceSchemeHeader.FirstOrDefault((kv) => kv.Key.Equals("Fonts", StringComparison.OrdinalIgnoreCase)))
		{
			case { Value: KeyValues } keyValue:
				targetSourceSchemeFonts = keyValue.Value;
				break;
			case { Value: string } keyValue:
				targetSourceSchemeFonts = [];
				targetSourceSchemeHeader[targetSourceSchemeHeader.IndexOf(keyValue)] = new KeyValue
				{
					Key = keyValue.Key,
					Value = targetSourceSchemeHeader,
					Conditional = keyValue.Conditional
				};
				break;
			default:
				targetSourceSchemeFonts = [];
				targetSourceSchemeHeader.Add(new KeyValue
				{
					Key = "Fonts",
					Value = targetSourceSchemeFonts,
					Conditional = null
				});
				break;
		}

		foreach (string fontName in dependencies.SourceScheme.Fonts)
		{
			IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> fonts = sourceSourceScheme.GetFont(fontName);

			List<KeyValue> targetFonts = targetSourceSchemeFonts
				.Where((font) => font.Key.Equals(fontName, StringComparison.OrdinalIgnoreCase))
				.ToList();

			foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> font in sourceSourceScheme.GetFont(fontName))
			{
				KeyValue targetFont = targetFonts.FirstOrDefault((keyValue) => KeyValueComparer.KeyComparer.Equals(keyValue, font.Key));

				int fontIndex = targetSourceSchemeFonts.IndexOf(targetFont);

				if (fontIndex != -1)
				{
					targetFonts.Remove(targetFont);
					int index = fontIndex;

					targetSourceSchemeFonts[index] = new KeyValue
					{
						Key = font.Key.Key,
						Value = font.Value,
						Conditional = font.Key.Conditional
					};
				}
				else
				{
					targetSourceSchemeFonts.Add(new KeyValue
					{
						Key = font.Key.Key,
						Value = font.Value,
						Conditional = font.Key.Conditional
					});
				}
			}

			foreach (KeyValue e in targetFonts)
			{
				targetFonts.Remove(e);
			}
		}

		FilesHashSet seen = [];
		Dictionary<string, KeyValues> baseKeyValues = [];

		void RemoveBaseSchemeValues(string folderPath, IEnumerable<string> baseFiles)
		{
			foreach (string baseFile in baseFiles)
			{
				string relativePath = Path.GetRelativePath(".", Path.Join(folderPath, baseFile));
				KeyValues? keyValues = reader.TryReadKeyValues(target, relativePath);

				if (!seen.Add(relativePath) || keyValues == null) continue;

				KeyValues header = keyValues.Header("Scheme");

				IEnumerable<KeyValues> fontsLists = header
					.Where((kv) => kv.Key.Equals("Fonts", StringComparison.OrdinalIgnoreCase))
					.Select((kv) => kv.Value)
					.OfType<KeyValues>();

				foreach (KeyValues fontsList in fontsLists)
				{
					List<KeyValue> removeList = fontsList
						.Where((font) => dependencies.SourceScheme.Fonts.Contains(font.Key, StringComparer.OrdinalIgnoreCase))
						.ToList();

					foreach (KeyValue font in removeList)
					{
						fontsList.Remove(font);
					}
				}

				baseKeyValues[relativePath] = keyValues;

				string? directoryName = Path.GetDirectoryName(relativePath);
				if (directoryName != null)
				{
					RemoveBaseSchemeValues(directoryName, keyValues.BaseFiles());
				}
			}
		}

		RemoveBaseSchemeValues(Path.Join(target.FolderPath, "resource"), targetSourceSchemeKeyValues.BaseFiles());

#if DEBUG
		if (dependencies.Files.Contains("resource\\sourcescheme.res"))
		{
			throw new UnreachableException("dependencies.Files Contains \"resource\\sourcescheme.res\"");
		}
#endif

		return (writer) =>
		{
			writer.Write("resource\\sourcescheme.res", targetSourceSchemeKeyValues);
			foreach (KeyValuePair<string, KeyValues> kvp in baseKeyValues)
			{
				writer.Write(kvp.Key, kvp.Value);
			}
		};
	}

	private static Action<HUDFileWriterService>? MergeLanguageTokens(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		if (dependencies.LanguageTokens.Count == 0)
		{
			return null;
		}

		reader.Require([
			(source, "resource\\chat_english.txt", FileType.VDF),
			(target, "resource\\chat_english.txt", FileType.VDF)
		]);

		static (KeyValues Root, KeyValues Tokens) LoadLanguageFile(HUDFileReaderService reader, HUD hud)
		{
			KeyValues keyValues = reader.ReadKeyValues(hud, "resource\\chat_english.txt");
			KeyValues languageFileHeader = keyValues.Header("lang");
			KeyValues languageTokens;

			switch (languageFileHeader.FirstOrDefault((kv) => kv.Key.Equals("Tokens", StringComparison.OrdinalIgnoreCase)))
			{
				case { Value: KeyValues } keyValue:
					languageTokens = keyValue.Value;
					break;
				case { Value: string } keyValue:
					languageTokens = [];
					languageFileHeader[languageFileHeader.IndexOf(keyValue)] = new KeyValue
					{
						Key = "Tokens",
						Value = languageTokens,
						Conditional = null
					};
					break;
				default:
					languageTokens = [];
					languageFileHeader.Add(new KeyValue
					{
						Key = "Tokens",
						Value = languageTokens,
						Conditional = null
					});
					break;
			}

			return (keyValues, languageTokens);
		}

		KeyValues sourceLanguageTokens = LoadLanguageFile(reader, source).Tokens;
		(KeyValues targetKeyValues, KeyValues targetLanguageTokens) = LoadLanguageFile(reader, target);

		foreach (string token in dependencies.LanguageTokens)
		{
			// Remove '#' from token
			string key = token[1..];

			List<KeyValue> targetTokens = targetLanguageTokens
				.Where((token) => token.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
				.ToList();

			foreach (KeyValue keyValue in sourceLanguageTokens.Where((kv) => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
			{
				KeyValue targetToken = targetTokens.FirstOrDefault((kv) => KeyValueComparer.KeyComparer.Equals(kv, keyValue));

				int tokenIndex = targetLanguageTokens.IndexOf(targetToken);

				if (tokenIndex != -1)
				{
					targetTokens.Remove(targetToken);
					int index = tokenIndex;

					targetLanguageTokens[index] = new KeyValue
					{
						Key = targetLanguageTokens[index].Key,
						Value = keyValue.Value,
						Conditional = targetLanguageTokens[index].Conditional,
					};
				}
				else
				{
					targetLanguageTokens.Add(keyValue);
				}
			}

			foreach (KeyValue t in targetTokens)
			{
				targetLanguageTokens.Remove(t);
			}
		}

		return (writer) =>
		{
			writer.Write(
				"resource\\chat_english.txt",
				targetKeyValues,
				new UnicodeEncoding(false, true) // LE BOM
			);
		};
	}

	private static Action<HUDFileWriterService>? MergeImages(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		reader.Require([
			..dependencies.Images.Select((image) => (source, $"{image}.vmt", FileType.VDF)  ),
		]);

		foreach (string image in dependencies.Images)
		{
			dependencies.Files.Add($"{image}.vmt");
			dependencies.Files.Add($"{image}.vtf");

			KeyValues? vmt = reader.TryReadKeyValues(source, $"{image}.vmt");
			if (vmt == null) continue;

			dependencies.Files.UnionWith(
				vmt
					.Header()
					.Where((kv) => kv.Key.Equals("$baseTexture", StringComparison.OrdinalIgnoreCase))
					.Select((kv) => kv.Value)
					.OfType<string>()
					.Select((value) => $"materials\\{value}{(value.EndsWith(".vtf") ? "" : ".vtf")}")
			);
		}

		return null;
	}

	private static Action<HUDFileWriterService>? MergeAudio(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		dependencies.Files.UnionWith(dependencies.Audio);
		return null;
	}

	private static Action<HUDFileWriterService>? MergeInfoVDF(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		reader.Require([
			(target, "info.vdf", FileType.VDF)
		]);

		KeyValues? keyValues = null;

		if (reader.TryReadKeyValues(target, "info.vdf") == null)
		{
			keyValues = [
				new KeyValue
				{
					Key = target.Name,
					Value = new KeyValues([new KeyValue { Key = "ui_version", Value = 3.ToString(), Conditional = null }]),
					Conditional = null
				}
			];
		}

		return (writer) =>
		{
			if (keyValues != null)
			{
				writer.Write("info.vdf", keyValues);
			}
		};
	}

	private static Action<HUDFileWriterService>? CopyFiles(HUDFileReaderService reader, HUD source, HUD target, Dependencies dependencies)
	{
		return (writer) =>
		{
			foreach (string relativePath in dependencies.Files.ToArray())
			{
				writer.Copy(source, relativePath);
			}
		};
	}
}
