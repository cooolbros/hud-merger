using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HUDMerger.Models;
using HUDMergerVDF;
using HUDMergerVDF.Models;

namespace HUDMerger;

public static class Utilities
{
	/// <summary>
	/// Determines whether the specified file exists. Does not throw an error if any folder in the path doesn't exist.
	/// </summary>
	public static bool TestPath(string filePath)
	{
		string folderPath = "";
		string[] folders = Regex.Split(filePath, "[\\/]+");
		folders[^1] = "";
		for (int i = 0; i < folders.Length - 1; i++)
		{
			folderPath += folders[i] + "\\";
			if (!Directory.Exists(folderPath))
			{
				return false;
			}
		}
		if (!File.Exists(filePath))
		{
			return false;
		}
		return true;
	}

	public static Dictionary<string, dynamic> VDFTryParse(string filePath, VDFParseOptions options = default)
	{
		try
		{
			Dictionary<string, dynamic> obj = VDF.Parse(File.ReadAllText(filePath), options);
			return obj;
		}
		catch (Exception e)
		{
			throw new Exception($"Syntax error found in {filePath}, unable to merge!\r\n" + e.Message);
		}
	}

	public static Dictionary<string, List<HUDAnimation>> HUDAnimationsTryParse(string filePath)
	{
		try
		{
			Dictionary<string, List<HUDAnimation>> animations = HUDAnimations.Parse(File.ReadAllText(filePath));
			return animations;
		}
		catch (Exception e)
		{
			throw new Exception($"Syntax error found in {filePath}, unable to merge!\r\n" + e.Message);
		}
	}

	/// <summary>
	/// Copy a default HUD file to a specified HUD folder
	/// </summary>
	/// <param name="resourcePath">Relative path of file</param>
	/// <param name="hudPath">HUD root directory</param>
	public static void CopyResourceToHUD(string resourcePath, string hudPath)
	{
		Directory.CreateDirectory(Path.Join(hudPath, Path.GetDirectoryName(resourcePath)));
		File.Copy(Path.Join("Resources\\HUD", resourcePath), Path.Join(hudPath, resourcePath));
	}

	/// <summary>
	/// Overwrite scheme entries
	/// </summary>
	/// <param name="scheme1"></param>
	/// <param name="scheme2"></param>
	public static void OverWriteSchemeEntries(Dictionary<string, dynamic> scheme1, Dictionary<string, dynamic> scheme2)
	{
		scheme1.TryAdd("Scheme", new Dictionary<string, dynamic>());
		scheme2.TryAdd("Scheme", new Dictionary<string, dynamic>());

		foreach (KeyValuePair<string, dynamic> section in scheme2["Scheme"])
		{
			// GetDependencyValues() returns a scheme initialized with all sections
			if (section.Value.Count > 0)
			{
				scheme1["Scheme"].TryAdd(section.Key, new Dictionary<string, dynamic>());
				foreach (KeyValuePair<string, dynamic> schemeSectionEntry in section.Value)
				{
					scheme1["Scheme"][section.Key][schemeSectionEntry.Key] = schemeSectionEntry.Value;
				}
			}
		}
	}

	/// <summary>
	/// Check if a directory contains another directory
	/// </summary>
	/// <param name="parentDir"></param>
	/// <param name="subDir"></param>
	public static bool PathContainsPath(string parentDir, string subDir)
	{
		string relativeDirectory = Path.GetRelativePath(parentDir, subDir);
		return !relativeDirectory.StartsWith("..") && !Path.IsPathRooted(relativeDirectory);
	}
}
