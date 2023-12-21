using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HUDMerger.Extensions;

namespace HUDMerger.Models;

public partial class VPK
{
	[GeneratedRegex(@"_dir\.vpk$")]
	private static partial Regex VPKDirRegex();

	[GeneratedRegex(@"[/\\]+")]
	private static partial Regex PathSeparatorRegex();

	private readonly string _archivePath;

	public uint Signature { get; }
	public uint Version { get; }
	public uint TreeSize { get; }
	public uint FileDataSectionSize { get; }
	public uint ArchiveMD5SectionSize { get; }
	public uint OtherMD5SectionSize { get; }
	public uint SignatureSectionSize { get; }

	public Dictionary<string, VPKFile> Files { get; } = [];

	public record class VPKFile
	{
		public ushort ArchiveIndex { get; init; }
		public uint EntryOffset { get; init; }
		public uint EntryLength { get; init; }
	}

	public VPK(string path)
	{
		FileInfo file = new(path);
		_archivePath = Path.Join(file.DirectoryName, VPKDirRegex().Replace(file.Name, ""));

		using FileStream stream = File.Open(path, FileMode.Open);
		using BinaryReader reader = new(stream, Encoding.UTF8, false);

		Signature = reader.ReadUInt32();
		if (Signature != 1437209140)
		{
			throw new Exception("Signature != 1437209140");
		}

		Version = reader.ReadUInt32();
		if (Version != 2)
		{
			throw new Exception("Version != 2");
		}

		TreeSize = reader.ReadUInt32();

		FileDataSectionSize = reader.ReadUInt32();
		if (FileDataSectionSize != 0)
		{
			throw new Exception("FileDataSectionSize != 0");
		}

		ArchiveMD5SectionSize = reader.ReadUInt32();

		OtherMD5SectionSize = reader.ReadUInt32();
		if (OtherMD5SectionSize != 48)
		{
			throw new Exception("OtherMD5SectionSize != 48");
		}

		SignatureSectionSize = reader.ReadUInt32();
		if (SignatureSectionSize != 296)
		{
			throw new Exception("SignatureSectionSize != 296");
		}

		while (true)
		{
			string extension = reader.ReadNullTerminatedString();
			if (extension == "")
			{
				break;
			}

			while (true)
			{
				string folderPath = reader.ReadNullTerminatedString();
				if (folderPath == "")
				{
					break;
				}

				while (true)
				{
					string fileName = reader.ReadNullTerminatedString();
					if (fileName == "")
					{
						break;
					}

					uint crc = reader.ReadUInt32();
					ushort preloadBytes = reader.ReadUInt16();
					ushort archiveIndex = reader.ReadUInt16();
					uint entryOffset = reader.ReadUInt32();
					uint entryLength = reader.ReadUInt32();

					ushort terminator = reader.ReadUInt16();
					if (terminator != ushort.MaxValue)
					{
						throw new Exception("terminator != 255");
					}

					string key = $"{(folderPath != " " ? $"{folderPath}/" : "")}{fileName}.{extension}";
					Files[key] = new VPKFile { ArchiveIndex = archiveIndex, EntryOffset = entryOffset, EntryLength = entryLength };
				}
			}
		}
	}

	public byte[] Read(string filePath)
	{
		VPKFile entry = Files[PathSeparatorRegex().Replace(filePath, "/")];
		string vpkPath = $"{_archivePath}_{(entry.ArchiveIndex == short.MaxValue ? "dir" : entry.ArchiveIndex.ToString().PadLeft(3, '0'))}.vpk";
		using FileStream stream = File.Open(vpkPath, FileMode.Open);
		stream.Seek(entry.EntryOffset, SeekOrigin.Begin);
		byte[] bytes = new byte[entry.EntryLength];
		stream.Read(bytes, 0, (int)entry.EntryLength);
		return bytes;
	}
}
