using System;

namespace HUDAnimations.Models.Animations;

public class SetTexture : HUDAnimationBase
{
	public override string Type => nameof(SetTexture);
	public required string Element { get; init; }
	public required string Property { get; init; }
	public required string Texture { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Property)} {Print(Texture)} {Print(Delay)}" + PrintConditional();
	}
}
