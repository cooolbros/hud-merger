using System;
using System.Linq;

namespace HUDMergerVDF.Exceptions;

public class VDFSyntaxException : Exception
{
	public int Position;
	public int Line;
	public int Character;

	public VDFSyntaxException(string unexpectedToken, int position, int line, int character, string expectedValue = "") : base($"Unexpected \"{unexpectedToken}\" at position {position} (line {line + 1}, character {character + 1})! {(expectedValue != "" ? $"Are you missing a{(new char[] { 'a', 'e', 'i', 'o', 'u' }.Contains(expectedValue[0]) ? "n" : "")} {expectedValue}" : "")}")
	{
		Position = position;
		Line = line;
		Position = character;
	}
}
