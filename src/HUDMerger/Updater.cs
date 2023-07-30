using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using HUDMerger.Models;
using HUDMerger.ViewModels;

namespace HUDMerger;

static class Updater
{
	public static Task Update(bool download, bool extract)
	{
		if (download)
		{
			Task t1 = DownloadResource<ClientschemeDependencies>("Clientscheme.json").ContinueWith((Task<ClientschemeDependencies> result) => ClientschemeDependencies.Properties = result.Result);
			Task t2 = DownloadResource<HUDPanel[]>("Panels.json").ContinueWith((Task<HUDPanel[]> result) => MainWindowViewModel.HUDPanels = result.Result.Select(hudPanel => new HUDPanelViewModel(hudPanel)).ToArray());
			Task t3 = DownloadAndOrExtract(extract);

			return Task.WhenAll(new List<Task>() { t1, t2, t3 });
		}
		else if (extract)
		{
			return Task.WhenAll(Extract(JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.OpenRead("Resources\\HUDFiles.json"))));
		}

		return default(Task);
	}

	/// <summary>
	/// Try to download HUDFiles.json then extract files listed, if the download files extract files listed in the local version of HUDFiles.json
	/// </summary>
	/// <param name="extract"></param>
	/// <returns></returns>
	private static async Task DownloadAndOrExtract(bool extract)
	{
		try
		{
			Dictionary<string, string[]> vpkFiles = await DownloadResource<Dictionary<string, string[]>>("HUDFiles.json");
			if (extract)
			{
				Extract(vpkFiles);
			}
		}
		catch (Exception e)
		{
			System.Diagnostics.Debug.WriteLine(e.Message);
			if (extract)
			{
				await Task.WhenAll(Extract(JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.OpenRead("Resources\\HUDFiles.json"))));
			}
		}
	}

	/// <summary>
	/// Extract files from a vpk using vpk.exe
	/// </summary>
	/// <param name="vpkFiles"></param>
	private static IEnumerable<Task> Extract(Dictionary<string, string[]> vpkFiles)
	{
		List<Task> tasks = new List<Task>();
		foreach (KeyValuePair<string, string[]> vpkFile in vpkFiles)
		{
			List<string> args = new List<string>
				{
					$"x",
					$"\"{Properties.Settings.Default.Team_Fortress_2_Folder}\\{vpkFile.Key}\""
				};

			foreach (string file in vpkFile.Value)
			{
				Directory.CreateDirectory(Path.Join("Resources\\HUD", Path.GetDirectoryName(file)));
				args.Add($"\"{file}\"");
			}

			ProcessStartInfo processStartInfo = new($"{Properties.Settings.Default.Team_Fortress_2_Folder}\\bin\\vpk.exe")
			{
				WorkingDirectory = $"{Directory.GetCurrentDirectory()}\\Resources\\HUD",
				Arguments = string.Join(' ', args)
			};

			Process process = Process.Start(processStartInfo);
			tasks.Add(process.WaitForExitAsync());
		}
		return tasks;
	}

	private static async Task<T> DownloadResource<T>(string resourcePath)
	{
		string json = await new HttpClient().GetStringAsync($"{Properties.Resources.ResourcesURL}/{resourcePath}");
		File.WriteAllText($"Resources\\{resourcePath}", json);
		return JsonSerializer.Deserialize<T>(json);
	}
}
