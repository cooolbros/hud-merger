using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace hud_merger
{
	static class Updater
	{
		public static void Update(bool Download, bool Extract)
		{
			BackgroundWorker backgroundWorker = new();

			backgroundWorker.DoWork += async (object sender, DoWorkEventArgs e) =>
			{
				if (Download)
				{
					List<string> URLs = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up ? new()
					{
						Properties.Resources.PanelsURL,
						Properties.Resources.ClientschemeURL
					} : new();

					System.Net.Http.HttpClient client = new();

					foreach (string URL in URLs)
					{
						Uri uri = new(URL);
						string FilePath = string.Join('\\', uri.LocalPath.Split('/')[^2..]);
						File.WriteAllText(FilePath, await client.GetStringAsync(URL));
					}

					// File will update after restart
				}

				if (Extract)
				{
					string VPK = Properties.Settings.Default.Team_Fortress_Folder + "\\bin\\vpk.exe";
					string TF2MISC = Properties.Settings.Default.Team_Fortress_Folder + "\\tf\\tf2_misc_dir.vpk";

					if (!File.Exists(VPK))
					{
						MessageBox.Show("Could not find vpk.exe");
					}

					if (!File.Exists(TF2MISC))
					{
						MessageBox.Show("Could not find tf2_misc_dir.vpk");
					}

					ProcessStartInfo info = new(VPK)
					{
						WorkingDirectory = Directory.GetCurrentDirectory() + "\\Resources\\HUD",
						Arguments = string.Join(' ', new string[]
						{
							"x",
							$"\"{TF2MISC}\"",
							"\"resource/clientscheme.res\"",
							"\"scripts/hudanimations_manifest.txt\"",
							"\"scripts/hudlayout.res\""
						})
					};

					Process.Start(info);
				}
			};

			backgroundWorker.RunWorkerAsync();
		}
	}
}