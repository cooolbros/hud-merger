using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HUDMerger.Models;

/// <summary>
/// Stores sets of clientscheme variable names
/// </summary>
/// <remarks>
/// This class is used when adding dependencies required by HUD files when merging
/// </remarks>
public class ClientschemeDependencies
{
	public static ClientschemeDependencies Properties = JsonSerializer.Deserialize<ClientschemeDependencies>(File.ReadAllText("Resources\\Clientscheme.json"));
	public HashSet<string> Colours { get; set; } = new();
	public HashSet<string> Borders { get; set; } = new();
	public HashSet<string> Fonts { get; set; } = new();
	public FilesHashSet Images { get; set; } = new();
	public FilesHashSet Audio { get; set; } = new();

	/// <summary>
	/// Add all dependencies referenced in files
	/// </summary>
	/// <param name="hudPath">Absolute path to HUD folder</param>
	/// <param name="files">Files to add dependencies in (Additional #base files will be added to this set)</param>
	public void Add(string hudPath, FilesHashSet files)
	{
		foreach (string hudFile in files.ToArray())
		{
			Add(hudPath, hudFile, files);
		}
	}

	/// <summary>
	/// Add dependencies referenced in a file
	/// </summary>
	/// <param name="hudPath">Absolute path to HUD folder</param>
	/// <param name="hudFile">Relative path to file</param>
	/// <param name="files">FilesHashSet to add #base files to</param>
	public void Add(string hudPath, string hudFile, FilesHashSet files)
	{
		string absoluteFilePath = Path.Join(hudPath, hudFile);
		if (!File.Exists(absoluteFilePath))
		{
			System.Diagnostics.Debug.WriteLine("Could not find " + absoluteFilePath);
			return;
		}

		Dictionary<string, dynamic> obj = Utilities.VDFTryParse(absoluteFilePath);

		// #base
		if (obj.ContainsKey("#base"))
		{
			FilesHashSet baseFiles = new();
			if (obj["#base"].GetType() == typeof(List<dynamic>))
			{
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

			string relativeFolderPath = Path.GetDirectoryName(hudFile);

			foreach (string baseFile in baseFiles)
			{
				string baseFileRelativePath = Path.Join(relativeFolderPath, baseFile);
				Add(hudPath, baseFileRelativePath, files);
				files.Add(baseFileRelativePath);
			}
		}

		Add(obj);
	}

	/// <summary>
	/// Add dependencies referenced in a Dictionary
	/// </summary>
	/// <param name="obj">Object</param>
	public void Add(Dictionary<string, dynamic> obj)
	{
		// Look at primitive properties and add matches to Dependencies Dictionary
		void IterateDictionary(Dictionary<string, dynamic> obj)
		{
			foreach (string key in obj.Keys)
			{
				if (obj[key].GetType() == typeof(Dictionary<string, dynamic>))
				{
					IterateDictionary(obj[key]);
				}
				else
				{
					// Colours
					foreach (string colourProperty in Properties.Colours)
					{
						if (key.ToLower().Contains(colourProperty))
						{
							if (obj[key].GetType() == typeof(List<dynamic>))
							{
								foreach (dynamic duplicateKey in obj[key])
								{
									if (duplicateKey.GetType() == typeof(string))
									{
										Colours.Add(duplicateKey);
									}
								}
							}
							else if (obj[key].GetType() == typeof(string))
							{
								Colours.Add(obj[key]);
							}
						}
					}

					// Borders
					foreach (string borderProperty in Properties.Borders)
					{
						if (key.ToLower().Contains(borderProperty))
						{
							if (obj[key].GetType() == typeof(List<dynamic>))
							{
								foreach (dynamic duplicateKey in obj[key])
								{
									if (duplicateKey.GetType() == typeof(string))
									{
										Borders.Add(duplicateKey);
									}
								}
							}
							else if (obj[key].GetType() == typeof(string))
							{
								Borders.Add(obj[key]);
							}
						}
					}

					// Fonts
					foreach (string fontProperty in Properties.Fonts)
					{
						if (key.ToLower().Contains(fontProperty))
						{
							if (obj[key].GetType() == typeof(List<dynamic>))
							{
								foreach (dynamic duplicateKey in obj[key])
								{
									if (duplicateKey.GetType() == typeof(string))
									{
										Fonts.Add(duplicateKey);
									}
								}
							}
							else if (obj[key].GetType() == typeof(string))
							{
								Fonts.Add(obj[key]);
							}
						}
					}

					// Images
					foreach (string imageProperty in Properties.Images)
					{
						if (key.ToLower().Contains(imageProperty))
						{
							if (obj[key].GetType() == typeof(List<dynamic>))
							{
								foreach (dynamic duplicateKey in obj[key])
								{
									if (duplicateKey.GetType() == typeof(string))
									{
										Images.Add($"materials\\vgui\\{duplicateKey}");
									}
								}
							}
							else if (obj[key].GetType() == typeof(string))
							{
								Images.Add($"materials\\vgui\\{obj[key]}");
							}
						}
					}

					// Audio
					foreach (string audioProperty in Properties.Audio)
					{
						if (key.ToLower().Contains(audioProperty))
						{
							if (obj[key].GetType() == typeof(List<dynamic>))
							{
								foreach (dynamic duplicateKey in obj[key])
								{
									if (duplicateKey.GetType() == typeof(string))
									{
										Audio.Add($"sound\\{duplicateKey}");
									}
								}
							}
							else if (obj[key].GetType() == typeof(string))
							{
								Audio.Add($"sound\\{obj[key]}");
							}
						}
					}
				}
			}
		}

		IterateDictionary(obj);
	}
}
