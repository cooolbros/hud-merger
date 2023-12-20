using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using HUDMerger.Extensions;
using VDF;
using VDF.Models;

namespace HUDMerger.Models;

public class Dependencies
{
	private static readonly Dependencies PropertyNames = JsonSerializer.Deserialize<Dependencies>(File.ReadAllText("Resources\\Dependencies.json"))!;

	public SchemeDependencies ClientScheme { get; init; } = new();
	public SchemeDependencies SourceScheme { get; init; } = new();
	public HashSet<string> HUDLayout { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> Events { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> LanguageTokens { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public FilesHashSet Images { get; init; } = [];
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
		Audio.UnionWith(other.Audio);
		Files.UnionWith(other.Files);
	}

	public void Add(string hudPath)
	{
		foreach (string relativePath in Files)
		{
			Add(new FileInfo(Path.Join(hudPath, relativePath)));
		}
	}

	public void Add(FileInfo file)
	{
		if (!file.Exists) return;

		try
		{
			KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(file.FullName));
			Add(keyValues);

			foreach (string basePath in keyValues.BaseFiles())
			{
				FileInfo baseFile = new(Path.Combine(file.DirectoryName!, basePath));
				Add(baseFile);
			}
		}
		catch
		{
		}
	}

	public void Add(KeyValues keyValues)
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
				if (keyValue.Key.Contains(languageProperty, StringComparison.OrdinalIgnoreCase))
				{
					LanguageTokens.Add(keyValue.Value);
				}
			}

			// Images
			foreach (string imageProperty in PropertyNames.Images)
			{
				if (keyValue.Key.Contains(imageProperty, StringComparison.OrdinalIgnoreCase))
				{
					Images.Add($"materials\\vgui\\{keyValue.Value}");
				}
			}

			// Audio
			foreach (string audioProperty in PropertyNames.Audio)
			{
				if (keyValue.Key.Contains(audioProperty, StringComparison.OrdinalIgnoreCase))
				{
					Audio.Add($"sound\\{keyValue.Value}");
				}
			}
		});
	}
}
