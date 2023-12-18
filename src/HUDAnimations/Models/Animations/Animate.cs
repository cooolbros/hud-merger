using System;

namespace HUDAnimations.Models.Animations;

public class Animate : HUDAnimationBase
{
	public override string Type => nameof(Animate);
	public required string Element { get; init; }
	public required string Property { get; init; }
	public required string Value { get; init; }
	public required InterpolatorBase Interpolator { get; init; }
	public required float Delay { get; init; }
	public required float Duration { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Property)} {Print(Value)} {Interpolator} {Delay} {Duration}" + PrintConditional();
	}
}
