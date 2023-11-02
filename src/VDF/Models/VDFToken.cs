using System;

namespace VDF.Models;

public readonly record struct VDFToken
{
	public required VDFTokenType Type { get; init; }
	public required string Value { get; init; }
}
