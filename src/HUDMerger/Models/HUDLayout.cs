using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HUDMergerVDF;

namespace HUDMerger.Models;

public class HUDLayout
{
	private readonly Dictionary<string, Dictionary<string, dynamic>> Entries = new(StringComparer.OrdinalIgnoreCase);

	public HUDLayout(HUD hud)
	{
		void ReadFile(string filePath, Dictionary<string, dynamic> obj)
		{
			string[] baseFiles = obj.ContainsKey("#base")
				? obj["#base"] is List<dynamic> baseFilesList
					? baseFilesList.Select((baseFile) => (string)baseFile).ToArray()
					: new string[] { (string)obj["#base"] }
				: null;

			foreach (KeyValuePair<string, dynamic> kv in obj)
			{
				if (kv.Value is Dictionary<string, dynamic> header)
				{
					foreach (KeyValuePair<string, dynamic> hudLayoutEntry in header)
					{
						if (!Entries.ContainsKey(hudLayoutEntry.Key))
						{
							Entries[hudLayoutEntry.Key] = new Dictionary<string, dynamic>();
						}

						Dictionary<string, dynamic> entry = Entries[hudLayoutEntry.Key];

						if (hudLayoutEntry.Value is Dictionary<string, dynamic> properties)
						{
							foreach (KeyValuePair<string, dynamic> property in properties)
							{
								if (!entry.ContainsKey(property.Key))
								{
									entry[property.Key] = property.Value;
								}
							}
						}
					}
				}
			}

			if (baseFiles == null)
			{
				return;
			}

			string folderPath = Path.GetDirectoryName(filePath);

			foreach (string baseFile in baseFiles)
			{
				string baseFilePath = Path.Join(folderPath, baseFile);

				if (!Utilities.TestPath(baseFilePath))
				{
					continue;
				}

				Dictionary<string, dynamic> baseObj = Utilities.VDFTryParse(baseFilePath);
				ReadFile(baseFilePath, baseObj);
			}
		}

		string HUDLayoutPath = Path.Join(hud.FolderPath, "scripts\\hudlayout.res");

		Dictionary<string, dynamic> hudLayoutObj = Utilities.VDFTryParse(HUDLayoutPath);

		ReadFile(HUDLayoutPath, hudLayoutObj);
	}

	public Dictionary<string, dynamic> this[string index] => Entries[index];
}
