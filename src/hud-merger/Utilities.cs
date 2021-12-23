using System;
using System.Collections.Generic;
using System.IO;
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
		/// Returns a Dictionary of all key/values in a file, including #base files
		/// </summary>
		public static Dictionary<string, dynamic> LoadAllControls(string filePath)
		{
			Dictionary<string, dynamic> origin = new();

			void AddControls(string filePath)
			{
				// Some HUDs deliberately #base nonexistant file paths for customisation
				Dictionary<string, dynamic> obj = File.Exists(filePath) ? VDFTryParse(filePath) : new();

				// #base
				if (obj.ContainsKey("#base"))
				{
					List<string> baseFiles = new();
					if (obj["#base"].GetType() == typeof(List<dynamic>))
					{
						// the VDF Parser gives us a List<dynamic> which becomes a List<object>
						// at runtime for some reason, when you iterate it can evaluate each item
						// and correctly and is able to assign string to string.
						foreach (dynamic baseFile in obj["#base"])
						{
							baseFiles.Add(baseFile);
						}
					}
					else
					{
						// Assume #base is a string
						baseFiles.Add(obj["#base"]);
					}

					string[] folders = filePath.Split("\\");
					// Remove File Name
					folders[^1] = "";
					foreach (string baseFile in baseFiles)
					{
						AddControls(String.Join('\\', folders) + baseFile);
					}
				}

				Utilities.Merge(origin, obj);
			}

			AddControls(filePath);
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
	}
}