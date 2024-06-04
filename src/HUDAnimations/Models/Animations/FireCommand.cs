using System;

namespace HUDAnimations.Models.Animations;

public class FireCommand : HUDAnimationBase
{
	public override string Type => nameof(FireCommand);
	public required float Delay { get; init; }
	public required string Command { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Delay)} {Print(Command)}" + PrintConditional();
	}
}
