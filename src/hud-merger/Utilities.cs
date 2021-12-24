using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HUDMergerVDF;
using HUDMergerVDF.Models;

namespace HUDMerger
{
	public static class Utilities
	{
		public static void Merge(Dictionary<string, dynamic> obj1, Dictionary<string, dynamic> obj2)
		{
			foreach (string i in obj1.Keys)
			{
				if (obj1[i].GetType() == typeof(Dictionary<string, dynamic>))
				{
					if (obj2.ContainsKey(i) && obj2[i].GetType() == typeof(Dictionary<string, dynamic>))
					{
						Merge(obj1[i], obj2[i]);
					}
				}
				else
				{
					if (obj2.ContainsKey(i))
					{
						obj1[i] = obj2[i];
					}
				}
			}
			foreach (string j in obj2.Keys)
			{
				if (!obj1.ContainsKey(j))
				{
					obj1[j] = obj2[j];
				}
			}
		}

		/// <summary>
		/// Loads controls from a file the same way TF2 does.
		/// </summary>
		public static Dictionary<string, dynamic> LoadControls(string hudRoot, string relativeFilePath)
		{
			// #base files get loaded in order, with keys from the topmost files being used,
			// then the keyvalues from the original file get applied over everything.

			// values that are strings in base files will get overrided by objects
			// loaded in a higher priority file

			// values that are objects in base files will get overrided by string
			// loaded in a higher priority file

			// multiple objects in the same #base file will not override previous properties set,
			// but multiple objects in the same origin file will overrie previous properties set.
			//
			// file1.res:
			// Container
			// {
			//     Element
			//     {
			//         "xpos"                "10"
			//         "bgcolor_override"    "0 255 0 255"
			//     }
			//     Element1          StringValue
			// }
			//
			// file2.res:
			// Container
			// {
			//     Element
			//     {
			//         "xpos"                "20"
			//     }
			// }
			//
			//  Origin file:
			// #base file1.res
			// #base file2.res
			// Container
			// {
			//     Element
			//     {
			//         "bgcolor_override"    "255 0 0 255"
			//     }
			//     Element1
			//     {
			//          "ControlName"       "EditablePanel"
			//          "fieldName"         "Element1"
			//          "xpos"              "0"
			//          "ypos"              "0"
			//          "zpos"              "10"
			//          "wide"              "100"
			//          "tall"              "100"
			//          "visible"           "1"
			//          "enabled"           "1"
			//          "bgcolor_override"  "255 100 0 255"
			//     }
			// }
			//
			// The origin file will be loaded as:
			// Container
			// {
			//     Element
			//     {
			//         "xpos"                "20"
			//         "bgcolor_override"    "255 0 0 255"
			//     }
			//     Element1
			//     {
			//          "ControlName"       "EditablePanel"
			//          "fieldName"         "Element1"
			//          "xpos"              "0"
			//          "ypos"              "0"
			//          "zpos"              "10"
			//          "wide"              "100"
			//          "tall"              "100"
			//          "visible"           "1"
			//          "enabled"           "1"
			//          "bgcolor_override"  "255 100 0 255"
			//     }
			// }
			//

			Dictionary<string, dynamic> origin = new();

			void AddControls(string _folderPath, string _fileName, bool overrideKeys)
			{
				string _filePath = Path.Join(_folderPath, _fileName);
				Dictionary<string, dynamic> obj;
				if (File.Exists(_filePath))
				{
					obj = VDFTryParse(_filePath);
				}
				else if (File.Exists(Path.Join("Resources\\HUD", _filePath)))
				{
					System.Diagnostics.Debugger.Break();

					obj = VDFTryParse(Path.Join("Resources\\HUD", _filePath));
				}
				else
				{
					obj = new();
				}

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

					string folderName = Path.GetDirectoryName(_filePath);

					foreach (string baseFile in baseFiles)
					{
						AddControls(folderName, baseFile, false);
					}

					obj.Remove("#base");
				}

				Merge(origin, obj, overrideKeys);
			}

			Dictionary<string, dynamic> Merge(Dictionary<string, dynamic> obj1, Dictionary<string, dynamic> obj2, bool overrideKeys)
			{
				foreach (string i in obj2.Keys)
				{
					if (obj1.ContainsKey(i))
					{
						if (obj1[i].GetType() == typeof(Dictionary<string, dynamic>) && obj2[i].GetType() == typeof(Dictionary<string, dynamic>))
						{
							Merge(obj1[i], obj2[i], overrideKeys);
						}
						else
						{
							if (overrideKeys)
							{
								obj1[i] = obj2[i];
							}
						}
					}
					else
					{
						if (obj2[i] is List<dynamic> items)
						{
							obj1[i] = items.Aggregate((a, b) => a.GetType() == typeof(Dictionary<string, dynamic>) ? Merge(obj1[i], obj2[i], overrideKeys) : b);
						}
						else
						{
							// We dont need overrideKeys to write to object
							obj1[i] = obj2[i];
						}
					}
				}

				return obj1;
			}

			FileInfo fileInfo = new FileInfo(Path.Join(hudRoot, relativeFilePath));
			AddControls(fileInfo.Directory.FullName, fileInfo.Name, true);
			return origin;
		}

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
}