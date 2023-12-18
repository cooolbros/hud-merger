using System;

namespace HUDAnimations.Models.Animations;

public class RunEventChild : HUDAnimationBase
{
	public override string Type => nameof(RunEventChild);
	public required string Element { get; init; }
	public required string Event { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Print(Event)} {Delay}" + PrintConditional();
	}
}
