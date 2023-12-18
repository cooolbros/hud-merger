using System;

namespace HUDAnimations.Models.Animations;

public class StopEvent : HUDAnimationBase
{
	public override string Type => nameof(StopEvent);
	public required string Event { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Event)} {Delay}" + PrintConditional();
	}
}
