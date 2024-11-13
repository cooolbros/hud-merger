using System;

namespace HUDAnimations.Models.Animations;

public class SetFont : HUDAnimationBase
{
	public override string Type => nameof(SetFont);
	public required string Element { get; init; }
	public required string Property { get; init; }
	public required string Font { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Property)} {Print(Font)} {Print(Delay)}" + PrintConditional();
	}
}
