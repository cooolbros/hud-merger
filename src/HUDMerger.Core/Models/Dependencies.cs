using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HUDMerger.Core.Extensions;
using HUDMerger.Core.Services;
using VDF;
using VDF.Models;

namespace HUDMerger.Core.Models;

public class Dependencies
{
	private static readonly Dependencies PropertyNames = JsonSerializer.Deserialize<Dependencies>(File.ReadAllText("Resources\\Dependencies.json"))!;

	public SchemeDependencies ClientScheme { get; init; } = new();
	public SchemeDependencies SourceScheme { get; init; } = new();
	public HashSet<string> HUDLayout { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> Events { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> LanguageTokens { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public FilesHashSet Images { get; init; } = [];
	public FilesHashSet PreloadImages { get; init; } = [];
	public FilesHashSet Audio { get; init; } = [];
	public FilesHashSet Files { get; init; } = [];

	public Dependencies()
	{
	}

	public Dependencies(IEnumerable<Dependencies> collection) : base()
	{
		foreach (Dependencies dependencies in collection)
		{
			UnionWith(dependencies);
		}
	}

	public void UnionWith(Dependencies other)
	{
		ClientScheme.UnionWith(other.ClientScheme);
		SourceScheme.UnionWith(other.SourceScheme);
		HUDLayout.UnionWith(other.HUDLayout);
		Events.UnionWith(other.Events);
		LanguageTokens.UnionWith(other.LanguageTokens);
		Images.UnionWith(other.Images);
		PreloadImages.UnionWith(other.PreloadImages);
		Audio.UnionWith(other.Audio);
		Files.UnionWith(other.Files);
	}

	public void Add(IHUDFileReaderService reader, HUD hud)
	{
		reader.Require(Files.Select((file) => (hud, file, FileType.VDF)));

		foreach (string relativePath in Files.ToArray())
		{
			Add(reader, hud, relativePath);
		}
	}

	public void Add(IHUDFileReaderService reader, HUD hud, string relativePath)
	{
		try
		{
			KeyValues? keyValues = reader.TryReadKeyValues(hud, relativePath);
			if (keyValues != null)
			{
				Add(keyValues);

				foreach (string basePath in keyValues.BaseFiles())
				{
					string? directoryName = Path.GetDirectoryName(relativePath);
					if (directoryName != null)
					{
						string relativeBaseFilePath = Path.GetRelativePath(".", Path.Join(directoryName, basePath));
						Files.Add(relativeBaseFilePath);
						Add(reader, hud, relativeBaseFilePath);
					}
				}
			}
		}
		catch
		{
		}
	}

	public void Add(IEnumerable<KeyValue> keyValues)
	{
		keyValues.ForAll((KeyValue keyValue) =>
		{
			if (keyValue.Value is KeyValues) return;

			// Colours
			foreach (string colourProperty in PropertyNames.ClientScheme.Colours)
			{
				if (keyValue.Key.Contains(colourProperty, StringComparison.OrdinalIgnoreCase))
				{
					ClientScheme.Colours.Add(keyValue.Value);
				}
			}

			// Borders
			foreach (string borderProperty in PropertyNames.ClientScheme.Borders)
			{
				if (keyValue.Key.Contains(borderProperty, StringComparison.OrdinalIgnoreCase))
				{
					ClientScheme.Borders.Add(keyValue.Value);
				}
			}

			// Fonts
			foreach (string fontProperty in PropertyNames.ClientScheme.Fonts)
			{
				if (keyValue.Key.Contains(fontProperty, StringComparison.OrdinalIgnoreCase))
				{
					ClientScheme.Fonts.Add(keyValue.Value);
				}
			}

			// Language
			foreach (string languageProperty in PropertyNames.LanguageTokens)
			{
				if (keyValue.Value is string str && str.StartsWith('#') && keyValue.Key.Contains(languageProperty, StringComparison.OrdinalIgnoreCase))
				{
					LanguageTokens.Add(str);
				}
			}

			// Images
			foreach (string imageProperty in PropertyNames.Images)
			{
				if (keyValue.Key.Contains(imageProperty, StringComparison.OrdinalIgnoreCase))
				{
					Images.Add(Path.GetRelativePath(".", $"materials\\vgui\\{keyValue.Value}"));
				}
			}

			// Audio
			foreach (string audioProperty in PropertyNames.Audio)
			{
				if (keyValue.Key.Contains(audioProperty, StringComparison.OrdinalIgnoreCase))
				{
					Audio.Add(Path.GetRelativePath(".", $"sound\\{keyValue.Value}"));
				}
			}
		});
	}
}
