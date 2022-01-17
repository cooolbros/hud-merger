using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace HUDMerger.Models
{
	public class HUDBackupManager
	{
		public const string BackupDirectory = ".hud_backups";

		private readonly HUD HUD;

		public string HUDBackupPath { get; }

		/// <summary>
		/// List of absolute file paths to HUD backups
		/// </summary>
		public List<HUDBackup> Backups { get; set; }

		/// <summary>
		/// Whether this HUD has more than 0 backups
		/// </summary>
		public bool HasBackups => Backups.Count > 0;

		/// <summary>
		/// Returns the most recent HUD backup
		/// </summary>
		public HUDBackup MostRecent => Backups[^1];

		public HUDBackupManager(HUD hud)
		{
			HUD = hud;
			HUDBackupPath = Path.Join(Directory.GetCurrentDirectory(), BackupDirectory, hud.Name);

			if (!Directory.Exists(HUDBackupPath))
			{
				Directory.CreateDirectory(HUDBackupPath);
			}

			this.Backups = new DirectoryInfo(HUDBackupPath)
			.GetFiles()
			.OrderBy(file => file.CreationTime)
			.Select(file => new HUDBackup(HUD.Name, file))
			.ToList();
		}

		/// <summary>
		/// Create a new backup of the HUD directory
		/// </summary>
		public void Create()
		{
			string zipPath = Path.Join(HUDBackupPath, $"{HUD.Name}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.zip");
			ZipFile.CreateFromDirectory(HUD.FolderPath, zipPath);
			Backups.Add(new HUDBackup(HUD.Name, new FileInfo(zipPath)));
		}

		/// <summary>
		/// Restore the most recent backup to the HUD folder
		/// </summary>
		public void Restore()
		{
			if (Utilities.TestPath(HUD.FolderPath))
			{
				Directory.Delete(HUD.FolderPath);
			}

			ZipFile.ExtractToDirectory(MostRecent.FullName, HUD.FolderPath);
		}

		public void Delete(HUDBackup backup)
		{
			File.Delete(backup.FullName);
			Backups.Remove(backup);
		}
	}
}
