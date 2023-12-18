using System;

namespace HUDAnimations.Models.Interpolators;

public class PulseInterpolator : InterpolatorBase
{
	public required float Frequency { get; init; }

	public override string ToString() => $"Pulse {Frequency}";
}
