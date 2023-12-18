using System;
using System.Text.RegularExpressions;

namespace HUDAnimations.Models;

public abstract partial class HUDAnimationBase
{
	[GeneratedRegex("\\s")]
	private static partial Regex WhitespaceRegex();

	public abstract string Type { get; }
	public required string? Conditional { get; init; }

	protected static string Print(string str)
	{
		return WhitespaceRegex().IsMatch(str) ? $"\"{str}\"" : str;
	}

	protected string PrintConditional()
	{
		return Conditional != null ? $" {Conditional}" : "";
	}

	public abstract override string ToString();
}
