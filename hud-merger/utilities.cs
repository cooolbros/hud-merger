using System;
using System.Collections.Generic;
using System.IO;

namespace hud_merger
{
	public static class Utilities
	{
		public static void Merge(Dictionary<string, dynamic> Obj1, Dictionary<string, dynamic> Obj2)
		{
			foreach (string i in Obj1.Keys)
			{
				if (Obj1[i].GetType().Name.Contains("Dictionary"))
				{
					if (Obj2.ContainsKey(i) && Obj2[i].GetType().Name.Contains("Dictionary"))
					{
						Merge(Obj1[i], Obj2[i]);
					}
				}
				else
				{
					if (Obj2.ContainsKey(i))
					{
						Obj1[i] = Obj2[i];
					}
				}
			}
			foreach (string j in Obj2.Keys)
			{
				if (!Obj1.ContainsKey(j))
				{
					Obj1[j] = Obj2[j];
				}
			}
		}

		public static Dictionary<string, dynamic> LoadControls(string FilePath)
		{
			Dictionary<string, dynamic> Origin = new();

			void AddControls(string FilePath)
			{
				Dictionary<string, dynamic> Obj = VDF.Parse(File.ReadAllText(FilePath));

				// #base
				if (Obj.ContainsKey("#base"))
				{
					List<string> BaseFiles = new();
					if (Obj["#base"].GetType().Name.Contains("List"))
					{
						// the VDF Parser gives us a List<dynamic> which becomes a List<object>
						// at runtime for some reason, when you iterate it can evaluate each item
						// and correctly and is able to assign string to string.
						foreach (dynamic BaseFile in Obj["#base"])
						{
							BaseFiles.Add(BaseFile);
						}
					}
					else
					{
						// Assume #base is a string
						BaseFiles.Add(Obj["#base"]);
					}

					string[] Folders = FilePath.Split("\\");
					// Remove File Name
					Folders[Folders.Length - 1] = "";
					foreach (string BaseFile in BaseFiles)
					{
						AddControls(String.Join('\\', Folders) + BaseFile);
					}
				}

				Utilities.Merge(Origin, Obj);
			}

			AddControls(FilePath);
			return Origin;
		}
	}
}