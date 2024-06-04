using System;

namespace HUDAnimations.Models.Animations;

public class StopAnimation : HUDAnimationBase
{
	public override string Type => nameof(StopAnimation);
	public required string Element { get; init; }
	public required string Property { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Property)} {Print(Delay)}" + PrintConditional();
	}
}
