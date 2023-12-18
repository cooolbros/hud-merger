using System;

namespace HUDAnimations.Models.Animations;

public class StopPanelAnimations : HUDAnimationBase
{
	public override string Type => nameof(StopPanelAnimations);
	public required string Element { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Delay}" + PrintConditional();
	}
}
