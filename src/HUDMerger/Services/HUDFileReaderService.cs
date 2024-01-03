using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using HUDAnimations;
using HUDAnimations.Models;
using HUDMerger.Exceptions;
using HUDMerger.Extensions;
using HUDMerger.Models;
using VDF;
using VDF.Exceptions;
using VDF.Models;

namespace HUDMerger.Services;

public enum FileType : byte
{
	VDF = 0,
	HUDAnimations = 1
}

public partial class HUDFileReaderService
{
	private readonly Lazy<VPK> TF2MiscDirVPK = new(() => new(Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "tf\\tf2_misc_dir.vpk")));
	private readonly Lazy<VPK> PlatformMiscDirVPK = new(() => new(Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "platform\\platform_misc_dir.vpk")));

	private readonly Dictionary<string, dynamic?> Files = [];

	public void Require(IEnumerable<(HUD hud, string relativePath, FileType type)> filePaths)
	{
		HashSet<string> seen = [];
		List<Exception> exceptions = [];

		void Add(HUD hud, string relativePath, FileType type)
		{
			try
			{
				string absolutePath = App.PathSeparatorRegex().Replace(Path.GetFullPath(Path.Join(hud.FolderPath, relativePath)), "\\");
				if (!seen.Add(absolutePath)) return;

#if DEBUG
				if (absolutePath.Contains(".."))
				{
					throw new Exception("absolutePath Contains \"..\"");
				}
#endif

				string? text = new Func<string?>(() =>
				{
					if (File.Exists(absolutePath))
					{
						return File.ReadAllText(absolutePath);
					}

					if (TF2MiscDirVPK.Value.Exists(relativePath))
					{
						return Encoding.UTF8.GetString(TF2MiscDirVPK.Value.Read(relativePath));
					}

					// resource/sourceschemebase.res
					if (PlatformMiscDirVPK.Value.Exists(relativePath))
					{
						return Encoding.UTF8.GetString(PlatformMiscDirVPK.Value.Read(relativePath));
					}

					string tfPath = Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "tf", relativePath);
					if (File.Exists(tfPath))
					{
						return File.ReadAllText(tfPath);
					}

					string hl2Path = Path.Join(((App)Application.Current).Settings.Value.TeamFortress2Folder, "hl2", relativePath);
					if (File.Exists(hl2Path))
					{
						return File.ReadAllText(hl2Path);
					}

					return null;
				}).Invoke();

				if (text == null)
				{
					Files[absolutePath] = null;
					return;
				}

				switch (type)
				{
					case FileType.VDF:
						KeyValues keyValues = VDFSerializer.Deserialize(text);
						Files[absolutePath] = keyValues;
						string? directoryName = Path.GetDirectoryName(relativePath);
						foreach (string baseFile in keyValues.BaseFiles())
						{
							Add(hud, Path.GetRelativePath(".", Path.Join(directoryName, baseFile)), type);
						}
						break;
					case FileType.HUDAnimations:
						Files[absolutePath] = HUDAnimationsSerializer.Deserialize(text);
						break;
					default:
						throw new UnreachableException();
				}
			}
			catch (VDFSyntaxException exception)
			{
				exceptions.Add(new FileException(hud, relativePath, exception));
			}
		}

		foreach ((HUD hud, string relativePath, FileType type) in filePaths)
		{
			Add(hud, relativePath, type);
		}

		if (exceptions.Count != 0)
		{
			throw new AggregateException(exceptions);
		}
	}

	private dynamic? Read(HUD hud, string relativePath)
	{
		string absolutePath = Path.GetFullPath(Path.Join(hud.FolderPath, relativePath));

		if (Files.TryGetValue(absolutePath, out dynamic? value))
		{
			return value;
		}
		else
		{
#if DEBUG
			throw new ArgumentException($"The requested file {hud.Name} - \"{relativePath}\" was not previously read with {nameof(HUDFileReaderService)}.{nameof(Require)}()");
#endif
			throw new UnreachableException();
		}
	}

	public KeyValues ReadKeyValues(HUD hud, string relativePath)
	{
		return Read(hud, relativePath) switch
		{
			KeyValues keyValues => keyValues,
			HUDAnimationsFile => throw new ArgumentException($"The requested file {hud.Name} - \"{relativePath}\" was previously read with Require() as {FileType.HUDAnimations}"),
			null => throw new ArgumentException($"The requested file {hud.Name} - \"{relativePath}\" was previously read with Require() but the file does not exist"),
			_ => throw new UnreachableException()
		};
	}

	public KeyValues? TryReadKeyValues(HUD hud, string relativePath)
	{
		return Read(hud, relativePath) switch
		{
			KeyValues keyValues => keyValues,
			_ => null
		};
	}

	public HUDAnimationsFile ReadHUDAnimations(HUD hud, string relativePath)
	{
		return Read(hud, relativePath) switch
		{
			HUDAnimationsFile hudAnimations => hudAnimations,
			KeyValues => throw new ArgumentException($"The requested file {hud.Name} - \"{relativePath}\" was previously read with Require() as {FileType.VDF}"),
			null => throw new ArgumentException($"The requested file {hud.Name} - \"{relativePath}\" was previously read with Require() but the file does not exist"),
			_ => throw new UnreachableException()
		};
	}

	public HUDAnimationsFile? TryReadHUDAnimations(HUD hud, string relativePath)
	{
		return Read(hud, relativePath) switch
		{
			HUDAnimationsFile hudAnimations => hudAnimations,
			_ => null
		};
	}
}
