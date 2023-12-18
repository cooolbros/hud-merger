using System;

namespace HUDAnimations.Models.Animations;

public class SetInputEnabled : HUDAnimationBase
{
	public override string Type => nameof(SetInputEnabled);
	public required string Element { get; init; }
	public required bool Enabled { get; init; }
	public required float Delay { get; init; }

	public override string ToString()
	{
		return $"{Type} {Print(Element)} {Convert.ToByte(Enabled)} {Delay}" + PrintConditional();
	}
}
