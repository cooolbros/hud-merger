using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace hud_merger
{
	static class Updater
	{
		public static List<Task> Update(bool download, bool extract)
		{
			List<Task> tasks = new();

			if (download)
			{
				tasks.Add(Download<HUDPanel[]>(Properties.Resources.PanelsURL).ContinueWith((Task<HUDPanel[]> result) =>
				{
					if (result.Exception != null)
					{
						throw result.Exception;
					}
					MainWindow.HUDPanels = result.Result;
				}));

				tasks.Add(Download<ClientschemeDependencies>(Properties.Resources.ClientschemeURL).ContinueWith((Task<ClientschemeDependencies> result) =>
				{
					if (result.Exception != null)
					{
						throw result.Exception;
					}
					ClientschemeDependencies.Properties = result.Result;
				}));
			}

			if (extract)
			{
				string vpk = $"{Properties.Settings.Default.Team_Fortress_2_Folder}\\bin\\vpk.exe";
				string tf2Misc = $"{Properties.Settings.Default.Team_Fortress_2_Folder}\\tf\\tf2_misc_dir.vpk";

				bool vpkExists = File.Exists(vpk);
				bool tf2MiscExists = File.Exists(tf2Misc);

				if (vpkExists && tf2MiscExists)
				{
					List<string> args = new()
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
						args.Add($"\"{filePath}\"");
					}

					ProcessStartInfo processStartInfo = new(vpk)
					{
						WorkingDirectory = $"{Directory.GetCurrentDirectory()}\\Resources\\HUD",
						Arguments = string.Join(' ', args),
					};

					Process process = Process.Start(processStartInfo);

					tasks.Add(process.WaitForExitAsync());

					process.Close();
					process.Dispose();
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

			return tasks;
		}

		private static async Task<T> Download<T>(string url)
		{
			string json = await new HttpClient().GetStringAsync(url);
			File.WriteAllText(string.Join('\\', new Uri(url).LocalPath.Split('/')[^2..]), json);
			return JsonSerializer.Deserialize<T>(json);
		}
	}
}