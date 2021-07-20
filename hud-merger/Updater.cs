using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace hud_merger
{
	static class Updater
	{
		public static void Update(bool download, bool extract)
		{
			BackgroundWorker backgroundWorker = new();

			backgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
			{
				if (download)
				{
					List<string> urls = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up ? new()
					{
						Properties.Resources.PanelsURL,
						Properties.Resources.ClientschemeURL
					} : new();

					HttpClient client = new();

					foreach (string url in urls)
					{
						Uri uri = new(url);
						string FilePath = string.Join('\\', uri.LocalPath.Split('/')[^2..]);

						client.GetAsync(url).ContinueWith((Task<HttpResponseMessage> Response) =>
						{
							if (Response.IsCompletedSuccessfully && Response.Result.IsSuccessStatusCode)
							{
								Response.Result.Content.ReadAsStringAsync().ContinueWith((Task<string> Result) =>
								{
									File.WriteAllText(FilePath, Result.Result);
								});
							}
						});
					}

					// File will update after restart
				}

				if (extract)
				{
					string vpk = Properties.Settings.Default.Team_Fortress_Folder + "\\bin\\vpk.exe";
					string tf2Misc = Properties.Settings.Default.Team_Fortress_Folder + "\\tf\\tf2_misc_dir.vpk";

					bool vpkExists = File.Exists(vpk);
					bool tf2MiscExists = File.Exists(tf2Misc);

					if (vpkExists && tf2MiscExists)
					{
						List<string> arguments = new()
						{
							"x",
							$"\"{tf2Misc}\""
						};

						List<string> hudFilePaths = new()
						{
							"resource/clientscheme.res",
							"scripts/hudanimations_manifest.txt",
							"scripts/hudlayout.res",
						};

						foreach (string filePath in hudFilePaths)
						{
							// Ensure vpk.exe is able to extract to folder
							Directory.CreateDirectory(Path.GetDirectoryName($"Resources\\HUD\\{filePath.Replace('/', '\\')}"));
							arguments.Add($"\"{filePath}\"");
						}

						ProcessStartInfo info = new(vpk)
						{
							WorkingDirectory = Directory.GetCurrentDirectory() + "\\Resources\\HUD",
							WindowStyle = ProcessWindowStyle.Hidden,
							Arguments = string.Join(' ', arguments)
						};

						Process.Start(info);
					}
					else
					{
						string[] paragraphs = new string[]
						{
							$"Could not find {(!vpkExists ? "vpk.exe" : "")}{(!vpkExists && !tf2MiscExists ? " and " : "")}{(!tf2MiscExists ? "tf2_misc_dir.vpk" : "")}!",
							"HUD Merger may not create HUDs correctly if TF2 has been updated",
							"This message can be supressed by disabling \"Extract required TF2 HUD files on startup\" in Files > Settings"
						};
						MessageBox.Show(string.Join("\r\n\r\n", paragraphs), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			};

			backgroundWorker.RunWorkerAsync();
		}
	}
}