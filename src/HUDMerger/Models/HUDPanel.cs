using System;
using System.Collections.Generic;

namespace HUDMerger.Models;

public record class HUDPanel
{
	public required string Name { get; init; }
	public required string Main { get; init; }
	public KeyValueLocation? RequiredKeyValue { get; init; }
	public required Dependencies Dependencies { get; init; }
	public HashSet<string>? Files { get; init; }
}
