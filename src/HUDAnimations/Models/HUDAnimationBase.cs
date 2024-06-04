using System;
using System.Globalization;
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

	protected static string Print(float num)
	{
		return num.ToString(CultureInfo.InvariantCulture);
	}

	protected string PrintConditional()
	{
		return Conditional != null ? $" {Conditional}" : "";
	}

	public abstract override string ToString();
}
