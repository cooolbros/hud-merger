using System;

namespace HUDAnimations.Models.Animations;

public class RunEvent : HUDAnimationBase
{
	public override string Type => nameof(RunEvent);
	public required string Event { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Event)} {Delay}" + PrintConditional();
	}
}
