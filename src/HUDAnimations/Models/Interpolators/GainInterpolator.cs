using System;

namespace HUDAnimations.Models.Interpolators;

public class GainInterpolator : InterpolatorBase
{
	public required float Bias { get; init; }

	public override string ToString() => $"Gain {Print(Bias)}";
}
