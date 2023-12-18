using System;

namespace HUDAnimations.Models.Interpolators;

public class BiasInterpolator : InterpolatorBase
{
	public required float Bias { get; init; }

	public override string ToString() => $"Bias {Bias}";
}
