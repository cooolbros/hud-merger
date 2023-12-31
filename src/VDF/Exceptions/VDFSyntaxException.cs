using System;
using VDF.Models;

namespace VDF.Exceptions;

public class VDFSyntaxException(VDFToken? unexpected, string[] expected, int index, int line, int character) : Exception($"Unexpected {(unexpected != null ? $"{unexpected?.Type} '{unexpected?.Value}'" : "EOF")} at position {index} (line {line + 1}, character {character + 1}). Expected {string.Join(" | ", expected)}.")
{
}
