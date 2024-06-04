using System;

namespace HUDAnimations.Models.Interpolators;

public class FlickerInterpolator : InterpolatorBase
{
	public required float Randomness { get; init; }

	public override string ToString() => $"Flicker {Print(Randomness)}";
}
