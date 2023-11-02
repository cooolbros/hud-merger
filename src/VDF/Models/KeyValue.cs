using System;

namespace VDF.Models;

public readonly record struct KeyValue
{
	public required string Key { get; init; }
	public required dynamic Value { get; init; }
	public required string? Conditional { get; init; }
}
