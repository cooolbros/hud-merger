using System;

namespace HUDAnimations.Models.Animations;

public class SetString : HUDAnimationBase
{
	public override string Type => nameof(SetString);
	public required string Element { get; init; }
	public required string Property { get; init; }
	public required string String { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Property)} {Print(String)} {Print(Delay)}" + PrintConditional();
	}
}
