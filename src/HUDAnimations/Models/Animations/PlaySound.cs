using System;

namespace HUDAnimations.Models.Animations;

public class PlaySound : HUDAnimationBase
{
	public override string Type => nameof(PlaySound);
	public required float Delay { get; init; }
	public required string Sound { get; init; }

	public override string ToString()
	{
		return $"{Type} {Delay} {Print(Sound)}" + PrintConditional();
	}
}
