using System;
using System.IO;
using System.Text;
using HUDAnimations;
using HUDAnimations.Models;
using HUDMerger.Models;
using VDF;
using VDF.Models;

namespace HUDMerger.Services;

public class HUDFileWriterService(string folderPath) : IHUDFileWriterService
{
	private void Write(string relativePath, string text, Encoding? encoding)
	{
		string absolutePath = Path.GetFullPath(Path.Join(folderPath, relativePath));

		string? directoryName = Path.GetDirectoryName(absolutePath);
		if (directoryName != null)
		{
			Directory.CreateDirectory(directoryName);
		}

		encoding ??= Encoding.UTF8;
		File.WriteAllText(absolutePath, text, encoding);
	}

	public void Write(string relativePath, KeyValues keyValues, Encoding? encoding = default)
	{
		Write(relativePath, VDFSerializer.Serialize(keyValues), encoding);
	}

	public void Write(string relativePath, HUDAnimationsFile keyValues, Encoding? encoding = default)
	{
		Write(relativePath, HUDAnimationsSerializer.Serialize(keyValues), encoding);
	}

	public void Copy(HUD source, string relativePath)
	{
		string sourceFileName = Path.GetFullPath(Path.Join(source.FolderPath, relativePath));
		string destFileName = Path.GetFullPath(Path.Join(folderPath, relativePath));

		if (File.Exists(sourceFileName))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(destFileName)!);
			File.Copy(sourceFileName, destFileName, true);
		}
		else
		{
			if (File.Exists(destFileName))
			{
				File.Delete(destFileName);
			}
		}
	}
}
