using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HUDMerger.Extensions;

public static class BinaryReaderExtensions
{
	public static string ReadNullTerminatedString(this BinaryReader reader)
	{
		List<byte> bytes = [];
		byte b;

		while ((b = reader.ReadByte()) != 0)
		{
			bytes.Add(b);
		}

		return Encoding.UTF8.GetString(bytes.ToArray());
	}
}
