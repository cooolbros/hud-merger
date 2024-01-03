using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMerger.Extensions;
using HUDMerger.Services;
using VDF;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public abstract class SchemeBase : IScheme
{
	private readonly Dictionary<KeyValue, string?> Colours = new(KeyValueComparer.KeyComparer);
	private readonly Dictionary<KeyValue, dynamic> Borders = new(KeyValueComparer.KeyComparer);
	private readonly Dictionary<KeyValue, HashSet<KeyValue>?> Fonts = new(KeyValueComparer.KeyComparer);

	public HashSet<KeyValue> CustomFontFiles { get; } = [];

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

	public SchemeBase(IHUDFileReaderService reader, HUD hud, string relativePath) : this(reader, hud, relativePath, reader.ReadKeyValues(hud, relativePath))
	{
	}

	public SchemeBase(IHUDFileReaderService reader, HUD hud, string relativePath, KeyValues keyValues)
	{
		static SchemeFile? ReadBaseFile(IHUDFileReaderService reader, HUD hud, string relativePath)
		{
			KeyValues? keyValues = reader.TryReadKeyValues(hud, relativePath);
			if (keyValues == null) return null;

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
						KeyValues borderValues => borderValues.ToHashSetRecursive(),
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
						KeyValues values => values.ToHashSetRecursive(),
						string => null,
						_ => throw new NotSupportedException(),
					};
				}
			}

			IEnumerable<KeyValue> customFontFiles = header
				.Where((kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Key, "CustomFontFiles") && kv.Value is KeyValues)
				.SelectMany((kv) => (KeyValues)kv.Value);
			foreach (KeyValue customFontFile in customFontFiles)
			{
				scheme.CustomFontFiles.Add(customFontFile);
			}

			foreach (string baseFile in keyValues.BaseFiles())
			{
				SchemeFile? baseScheme = ReadBaseFile(reader, hud, Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(relativePath), baseFile)));
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
					KeyValues borderValues => borderValues.ToHashSetRecursive(),
					_ => throw new NotSupportedException(),
				}
			);
		}

		foreach (KeyValue font in GetValueOrDefault(header, "Fonts"))
		{
			Fonts.TryAdd(font, font.Value is KeyValues value ? value.ToHashSetRecursive() : null);
		}

		foreach (KeyValue customFontFile in GetValueOrDefault(header, "CustomFontFiles"))
		{
			CustomFontFiles.Add(customFontFile);
		}

		foreach (string baseFile in keyValues.BaseFiles())
		{
			SchemeFile? baseScheme = ReadBaseFile(reader, hud, Path.GetRelativePath(".", Path.Join(Path.GetDirectoryName(relativePath), baseFile)));
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
					if (value is HashSet<KeyValue> existingFont && font.Value is HashSet<KeyValue> baseFont)
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

	public IEnumerable<KeyValuePair<KeyValue, string>> GetColour(string colourName)
	{
#pragma warning disable CS8619
		return Colours
			.Where((colour) => colour.Key.Key.Equals(colourName, StringComparison.OrdinalIgnoreCase) && colour.Value is not null);
#pragma warning restore CS8619
	}

	public void SetColour(IEnumerable<KeyValuePair<KeyValue, string>> colourValue)
	{
		foreach (KeyValuePair<KeyValue, string> colour in colourValue)
		{
			Colours[colour.Key] = colour.Value;
		}
	}

	public IEnumerable<KeyValuePair<KeyValue, dynamic>> GetBorder(string borderName)
	{
		return Borders
			.Where((border) => border.Key.Key.Equals(borderName, StringComparison.OrdinalIgnoreCase));
	}

	public void SetBorder(IEnumerable<KeyValuePair<KeyValue, dynamic>> borderValue)
	{
		foreach (KeyValuePair<KeyValue, dynamic> border in borderValue)
		{
			Borders[border.Key] = border.Value;
		}
	}

	public IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> GetFont(string fontName)
	{
#pragma warning disable CS8619
		return Fonts
			.Where((font) => font.Key.Key.Equals(fontName, StringComparison.OrdinalIgnoreCase) && font.Value is not null);
#pragma warning restore CS8619
	}

	public void SetFont(IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> fontValue)
	{
		foreach (KeyValuePair<KeyValue, HashSet<KeyValue>> font in fontValue)
		{
			Fonts[font.Key] = font.Value;
		}
	}

	public KeyValues ToKeyValues()
	{
		static KeyValues ConvertSchemeToKeyValues<T>(Dictionary<KeyValue, T> dictionary)
		{
			return new(
				dictionary
					.Where((kv) => kv.Value is not null)
					.Select((kv) => new KeyValue { Key = kv.Key.Key, Value = kv.Value!, Conditional = kv.Key.Conditional })
			);
		}

		KeyValues keyValues = [];

		if (Colours.Count != 0)
		{
			keyValues.Add(new KeyValue
			{
				Key = "Colors",
				Value = ConvertSchemeToKeyValues(Colours),
				Conditional = null
			});
		}

		if (Borders.Count != 0)
		{
			keyValues.Add(new KeyValue
			{
				Key = "Borders",
				Value = ConvertSchemeToKeyValues(Borders),
				Conditional = null
			});
		}

		if (Fonts.Count != 0)
		{
			keyValues.Add(new KeyValue
			{
				Key = "Fonts",
				Value = ConvertSchemeToKeyValues(Fonts),
				Conditional = null
			});
		}

		if (CustomFontFiles.Count != 0)
		{
			List<KeyValue> customFontFilesList = [..CustomFontFiles];
			customFontFilesList.Sort((a, b) =>
			{
				if (int.TryParse(a.Key, out int first) && int.TryParse(b.Key, out int second))
				{
					return first - second;
				}
				else
				{
					return (b.Conditional?.Length ?? 0) - (a.Conditional?.Length ?? 0);
				}
			});

			keyValues.Add(new KeyValue
			{
				Key = "CustomFontFiles",
				Value = customFontFilesList,
				Conditional = null
			});
		}

		return new KeyValues([
			new KeyValue {
				Key = "Scheme",
				Value = keyValues,
				Conditional = null
			}
		]);
	}
}
