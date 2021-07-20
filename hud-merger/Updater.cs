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
		public static void Update(bool Download, bool Extract)
		{
			BackgroundWorker backgroundWorker = new();

			backgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
			{
				if (Download)
				{
					List<string> URLs = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up ? new()
					{
						Properties.Resources.PanelsURL,
						Properties.Resources.ClientschemeURL
					} : new();

					HttpClient client = new();

					foreach (string URL in URLs)
					{
						Uri uri = new(URL);
						string FilePath = string.Join('\\', uri.LocalPath.Split('/')[^2..]);

						client.GetAsync(URL).ContinueWith((Task<HttpResponseMessage> Response) =>
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

				if (Extract)
				{
					string VPK = Properties.Settings.Default.Team_Fortress_Folder + "\\bin\\vpk.exe";
					string TF2MISC = Properties.Settings.Default.Team_Fortress_Folder + "\\tf\\tf2_misc_dir.vpk";

					bool VPKExists = File.Exists(VPK);
					bool TF2MISCExists = File.Exists(TF2MISC);

					if (VPKExists && TF2MISCExists)
					{
						List<string> Arguments = new()
						{
							"x",
							$"\"{TF2MISC}\""
						};

						List<string> HUDFilePaths = new()
						{
							"resource/clientscheme.res",
							"scripts/hudanimations_manifest.txt",
							"scripts/hudlayout.res",
						};

						foreach (string FilePath in HUDFilePaths)
						{
							// Ensure vpk.exe is able to extract to folder
							Directory.CreateDirectory(Path.GetDirectoryName($"Resources\\HUD\\{FilePath.Replace('/', '\\')}"));
							Arguments.Add($"\"{FilePath}\"");
						}

						ProcessStartInfo info = new(VPK)
						{
							WorkingDirectory = Directory.GetCurrentDirectory() + "\\Resources\\HUD",
							WindowStyle = ProcessWindowStyle.Hidden,
							Arguments = string.Join(' ', Arguments)
						};

						Process.Start(info);
					}
					else
					{
						string[] Paragraphs = new string[]
						{
							$"Could not find {(!VPKExists ? "vpk.exe" : "")}{(!VPKExists && !TF2MISCExists ? " and " : "")}{(!TF2MISCExists ? "tf2_misc_dir.vpk" : "")}!",
							"HUD Merger may not create HUDs correctly if TF2 has been updated",
							"This message can be supressed by disabling \"Extract required TF2 HUD files on startup\" in Files > Settings"
						};
						MessageBox.Show(string.Join("\r\n\r\n", Paragraphs), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			};

			backgroundWorker.RunWorkerAsync();
		}
	}
}