using System;

namespace HUDAnimations.Models.Animations;

public class SetVisible : HUDAnimationBase
{
	public override string Type => nameof(SetVisible);
	public required string Element { get; init; }
	public required bool Visible { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Convert.ToByte(Visible)} {Print(Delay)}" + PrintConditional();
	}
}
