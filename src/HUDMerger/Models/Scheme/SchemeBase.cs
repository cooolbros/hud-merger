using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMerger.Extensions;
using VDF;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public abstract class SchemeBase : IScheme
{
	public abstract string Type { get; }

	private readonly Dictionary<KeyValue, string?> Colours = new(KeyValueComparer.KeyComparer);
	private readonly Dictionary<KeyValue, dynamic> Borders = new(KeyValueComparer.KeyComparer);
	private readonly Dictionary<KeyValue, HashSet<KeyValue>?> Fonts = new(KeyValueComparer.KeyComparer);

	public List<KeyValue> CustomFontFiles { get; } = [];

	private record class SchemeFile
	{
		public readonly Dictionary<KeyValue, string?> Colours = new(KeyValueComparer.KeyComparer);
		public readonly Dictionary<KeyValue, dynamic> Borders = new(KeyValueComparer.KeyComparer);
		public readonly Dictionary<KeyValue, HashSet<KeyValue>?> Fonts = new(KeyValueComparer.KeyComparer);
		public readonly List<KeyValue> CustomFontFiles = [];
	}

	public SchemeBase()
	{
	}

	public SchemeBase(string folderPath)
	{
		static SchemeFile? ReadBaseFile(FileInfo file)
		{
			if (!file.Exists) return null;

			KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(file.FullName));
			KeyValues header = keyValues.Header();

			SchemeFile scheme = new();

			IEnumerable<KeyValue> colours = header
				.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "Colors") && kv.Value is KeyValues)
				.SelectMany((kv) => (KeyValues)kv.Value);
			foreach (KeyValue colour in colours)
			{
				scheme.Colours.TryAdd(colour, colour.Value is string value ? value : null);
			}

			IEnumerable<KeyValue> borders = header
				.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "Borders") && kv.Value is KeyValues)
				.SelectMany((kv) => (KeyValues)kv.Value);
			foreach (KeyValue border in borders)
			{
				if (scheme.Borders.TryGetValue(border, out dynamic? value))
				{
					if (value is HashSet<KeyValue> existingBorder && border.Value is KeyValues borderValues)
					{
						existingBorder.UnionWithRecursive(borderValues);
					}
				}
				else
				{
					scheme.Borders[border] = border.Value switch
					{
						string borderReference => borderReference,
						KeyValues borderValues => borderValues.ToHashSet(),
						_ => throw new NotSupportedException()
					};
				}
			}

			IEnumerable<KeyValue> fonts = header
				.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "Fonts") && kv.Value is KeyValues)
				.SelectMany((kv) => (KeyValues)kv.Value);
			foreach (KeyValue font in fonts)
			{
				if (scheme.Fonts.TryGetValue(font, out HashSet<KeyValue>? value))
				{
					if (value is HashSet<KeyValue> existingFont && font.Value is KeyValues fontValues)
					{
						existingFont.UnionWithRecursive(fontValues);
					}
				}
				else
				{
					scheme.Fonts[font] = font.Value switch
					{
						KeyValues values => values.ToHashSet(),
						string => null,
						_ => throw new NotSupportedException(),
					};
				}
			}

			IEnumerable<KeyValue> customFontFiles = header
				.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "CustomFontFiles") && kv.Value is KeyValues)
				.SelectMany((kv) => (KeyValues)kv.Value);
			foreach (KeyValue font in fonts)
			{
				scheme.CustomFontFiles.Add(font);
			}

			foreach (string baseFile in keyValues.BaseFiles())
			{
				SchemeFile? baseScheme = ReadBaseFile(new FileInfo(Path.Join(file.DirectoryName, baseFile)));
				if (baseScheme == null) continue;

				foreach (KeyValuePair<KeyValue, string?> colour in baseScheme.Colours)
				{
					scheme.Colours.TryAdd(colour.Key, colour.Value);
				}

				foreach (KeyValuePair<KeyValue, dynamic> border in baseScheme.Borders)
				{
					if (scheme.Borders.TryGetValue(border.Key, out dynamic? value))
					{
						if (value is HashSet<KeyValue> existingBorder && border.Value is IEnumerable<KeyValue> borderValues)
						{
							existingBorder.UnionWithRecursive(borderValues);
						}
					}
					else
					{
						scheme.Borders[border.Key] = border.Value;
					}
				}

				foreach (KeyValuePair<KeyValue, HashSet<KeyValue>?> font in baseScheme.Fonts)
				{
					if (scheme.Borders.TryGetValue(font.Key, out dynamic? value))
					{
						if (value is HashSet<KeyValue> existingFont && font.Value is IEnumerable<KeyValue> fontValues)
						{
							existingFont.UnionWithRecursive(fontValues);
						}
					}
					else
					{
						scheme.Fonts[font.Key] = font.Value;
					}
				}

				foreach (KeyValue customFontFile in baseScheme.CustomFontFiles)
				{
					scheme.CustomFontFiles.Add(customFontFile);
				}
			}

			return scheme;
		}

		static KeyValues GetValueOrDefault(KeyValues keyValues, string key) =>
			keyValues.FirstOrDefault((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, key)).Value is KeyValues v ? v : [];

		string schemePath = Path.Join(folderPath, $"resource/{Type}scheme.res");

		KeyValues keyValues = VDFSerializer.Deserialize(File.ReadAllText(File.Exists(schemePath) ? schemePath : $"Resources\\HUD\\resource\\{Type}scheme.res"));

		KeyValues header = keyValues.Header();

		foreach (KeyValue colour in GetValueOrDefault(header, "Colors"))
		{
			Colours.TryAdd(colour, colour.Value is string value ? value : null);
		}

		foreach (KeyValue border in GetValueOrDefault(header, "Borders"))
		{
			Borders.TryAdd(
				border,
				border.Value switch
				{
					string borderReference => borderReference,
					KeyValues borderValues => borderValues.ToHashSet(),
					_ => throw new NotSupportedException(),
				}
			);
		}

		foreach (KeyValue font in GetValueOrDefault(header, "Fonts"))
		{
			Fonts.TryAdd(font, font.Value is KeyValues value ? value.ToHashSet() : null);
		}

		foreach (KeyValue customFontFile in GetValueOrDefault(header, "CustomFontFiles"))
		{
			CustomFontFiles.Add(customFontFile);
		}

		foreach (string baseFile in keyValues.BaseFiles())
		{
			SchemeFile? baseScheme = ReadBaseFile(new FileInfo(Path.Join(Path.GetDirectoryName(schemePath), baseFile)));
			if (baseScheme == null) continue;

			foreach (KeyValuePair<KeyValue, string?> colour in baseScheme.Colours)
			{
				Colours.TryAdd(colour.Key, colour.Value);
			}

			foreach (KeyValuePair<KeyValue, dynamic> border in baseScheme.Borders)
			{
				if (Borders.TryGetValue(border.Key, out dynamic? value))
				{
					if (value is HashSet<KeyValue> existingBorder && border.Value is IEnumerable<KeyValue> baseBorder)
					{
						existingBorder.UnionWithRecursive(baseBorder);
					}
				}
				else
				{
					Borders[border.Key] = border.Value;
				}
			}

			foreach (KeyValuePair<KeyValue, HashSet<KeyValue>?> font in baseScheme.Fonts)
			{
				if (Fonts.TryGetValue(font.Key, out HashSet<KeyValue>? value))
				{
					if (value is HashSet<KeyValue> existingFont && font.Value is IEnumerable<KeyValue> baseFont)
					{
						existingFont.UnionWithRecursive(baseFont);
					}
				}
				else
				{
					Fonts[font.Key] = font.Value;
				}
			}

			foreach (KeyValue customFontFile in baseScheme.CustomFontFiles)
			{
				CustomFontFiles.Add(customFontFile);
			}
		}
	}

	public IEnumerable<KeyValue> GetColour(string colourName)
	{
		return Colours
			.Where((colour) => StringComparer.OrdinalIgnoreCase.Equals(colour.Key.Key, colourName) && colour.Value != null)
			.Select((colour) => new KeyValue
			{
				Key = colour.Key.Key,
				Value = colour.Value!,
				Conditional = colour.Key.Conditional
			});
	}

	public IEnumerable<KeyValue> GetBorder(string borderName)
	{
		return Borders
			.Where((border) => StringComparer.OrdinalIgnoreCase.Equals(border.Key.Key, borderName) && border.Value != null)
			.Select((border) => new KeyValue
			{
				Key = border.Key.Key,
				Value = border.Value!,
				Conditional = border.Key.Conditional
			});
	}

	public IEnumerable<KeyValue> GetFont(string fontName)
	{
		return Fonts
			.Where((font) => StringComparer.OrdinalIgnoreCase.Equals(font.Key.Key, fontName) && font.Value != null)
			.Select((font) => new KeyValue
			{
				Key = font.Key.Key,
				Value = font.Value!,
				Conditional = font.Key.Conditional
			});
	}
}
