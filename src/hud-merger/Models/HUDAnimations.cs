using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HUDMergerVDF;
using HUDMergerVDF.Exceptions;

// sample animation script
//
//
// commands:
//	Animate <panel name> <variable> <target value> <interpolator> <start time> <duration>
//		variables:
//			FgColor
//			BgColor
//			Position
//			Size
//			Blur		(hud panels only)
//			TextColor	(hud panels only)
//			Ammo2Color	(hud panels only)
//			Alpha		(hud weapon selection only)
//			SelectionAlpha  (hud weapon selection only)
//			TextScan	(hud weapon selection only)
//
//		interpolator:
//			Linear
//			Accel - starts moving slow, ends fast
//			Deaccel - starts moving fast, ends slow
//			Spline - simple ease in/out curve
//			Pulse - < freq > over the duration, the value is pulsed (cosine) freq times ending at the dest value (assuming freq is integral)
//			Flicker - < randomness factor 0.0 to 1.0 > over duration, each frame if random # is less than factor, use end value, otherwise use prev value
//			Gain - < bias > Lower bias values bias towards 0.5 and higher bias values bias away from it.
//			Bias - < bias > Lower values bias the curve towards 0 and higher values bias it towards 1.
//
//	RunEvent <event name> <start time>
//		starts another even running at the specified time
//
//	StopEvent <event name> <start time>
//		stops another event that is current running at the specified time
//
//	StopAnimation <panel name> <variable> <start time>
//		stops all animations refering to the specified variable in the specified panel
//
//	StopPanelAnimations <panel name> <start time>
//		stops all active animations operating on the specified panel
//
//  SetFont <panel name> <fontparameter> <fontname from scheme> <set time>
//
//	SetTexture <panel name> <textureidname> <texturefilename> <set time>
//
//  SetString <panel name> <string varname> <stringvalue> <set time>

namespace HUDMerger.Models
{
	public abstract class HUDAnimation
	{
		public abstract string Type { get; }
		public string OSTag { get; set; }
		public abstract override string ToString();

		protected string Print(string str)
		{
			return Regex.IsMatch(str, "\\s") ? $"\"{str}\"" : str;
		}

		protected string PrintOSTag()
		{
			return OSTag != null ? $" {OSTag}" : "";
		}
	}

	public class Animate : HUDAnimation
	{
		public override string Type => nameof(Animate);
		public string Element { get; set; }
		public string Property { get; set; }
		public string Value { get; set; }
		public string Interpolator { get; set; }
		public string Bias { get; set; }
		public string Frequency { get; set; }
		public string Delay { get; set; }
		public string Duration { get; set; }

		private string GetInterpolator()
		{
			if (Interpolator == "Gain" || Interpolator == "Bias")
			{
				return $"{Interpolator} {Bias}";
			}
			else if (Interpolator == "Pulse")
			{
				return $"{Interpolator} {Frequency}";
			}

			return Interpolator;
		}

		public override string ToString()
		{
			return $"{Type} {Print(Element)} {Print(Property)} {Print(Value)} {GetInterpolator()} {Delay} {Duration}" + PrintOSTag();
		}
	}

	public class RunEvent : HUDAnimation
	{
		public override string Type => nameof(RunEvent);
		public string Event { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Event)} {Delay}" + PrintOSTag();
		}
	}

	public class StopEvent : HUDAnimation
	{
		public override string Type => nameof(StopEvent);
		public string Event { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Event)} {Delay}" + PrintOSTag();
		}
	}

	public class SetVisible : HUDAnimation
	{
		public override string Type => nameof(SetVisible);
		public string Element { get; set; }
		public bool Visible { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Element)} {(Visible ? 1 : 0)} {Delay}" + PrintOSTag();
		}
	}

	public class FireCommand : HUDAnimation
	{
		public override string Type => nameof(FireCommand);
		public string Delay { get; set; }
		public string Command { get; set; }

		public override string ToString()
		{
			return $"{Type} {Delay} {Print(Command)}" + PrintOSTag();
		}
	}

	public class RunEventChild : HUDAnimation
	{
		public override string Type => nameof(RunEventChild);
		public string Element { get; set; }
		public string Event { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Element)} {Print(Event)} {Delay}" + PrintOSTag();
		}
	}

	public class SetInputEnabled : HUDAnimation
	{
		public override string Type => nameof(SetInputEnabled);
		public string Element { get; set; }
		public bool Enabled { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Element)} {(Enabled ? 1 : 0)} {Delay}" + PrintOSTag();
		}
	}

	public class PlaySound : HUDAnimation
	{
		public override string Type => nameof(PlaySound);
		public string Delay { get; set; }
		public string Sound { get; set; }

		public override string ToString()
		{
			return $"{Type} {Delay} {Print(Sound)}" + PrintOSTag();
		}
	}

	public class StopPanelAnimations : HUDAnimation
	{
		public override string Type => nameof(StopPanelAnimations);
		public string Element { get; set; }
		public string Delay { get; set; }

		public override string ToString()
		{
			return $"{Type} {Print(Element)} {Delay}" + PrintOSTag();
		}
	}

	public static class HUDAnimations
	{
		public static Dictionary<string, List<HUDAnimation>> Parse(string str)
		{
			VDFTokeniser tokeniser = new VDFTokeniser(str);

			Dictionary<string, List<HUDAnimation>> ParseFile()
			{
				Dictionary<string, List<HUDAnimation>> animations = new(StringComparer.OrdinalIgnoreCase);

				string currentToken = tokeniser.Read();

				while (currentToken != "EOF")
				{
					if (currentToken != "event")
					{
						throw new VDFSyntaxException(currentToken, tokeniser.Position, tokeniser.Line, tokeniser.Character, "event");
					}

					string eventName = tokeniser.Read();
					animations[eventName] = ParseEvent();
					currentToken = tokeniser.Read();
				}

				return animations;
			}

			List<HUDAnimation> ParseEvent()
			{
				List<HUDAnimation> _event = new();
				string nextToken = tokeniser.Read();

				if (nextToken != "{")
				{
					throw new VDFSyntaxException(nextToken, tokeniser.Position, tokeniser.Line, tokeniser.Character, "{");
				}

				while (nextToken != "}")
				{
					// NextToken is not a closing brace therefore it is the animation type
					// Pass the animation type to the animation
					nextToken = tokeniser.Read();
					if (nextToken != "}")
					{
						_event.Add(ParseAnimation(nextToken));
					}
				}

				return _event;
			}

			static string ParseInterpolator(string interpolator)
			{
				return interpolator[0].ToString().ToUpper() + interpolator.Substring(1, interpolator.Length - 1).ToLower();
			}

			void SetInterpolator(Animate animation)
			{
				string interpolator = tokeniser.Read().ToLower();
				switch (interpolator)
				{
					case "gain":
					case "bias":
						animation.Interpolator = ParseInterpolator(interpolator);
						animation.Bias = tokeniser.Read();
						break;
					case "pulse":
						animation.Interpolator = ParseInterpolator(interpolator);
						animation.Frequency = tokeniser.Read();
						break;
					default:
						animation.Interpolator = ParseInterpolator(interpolator);
						break;
				}
			}

			HUDAnimation ParseAnimation(string animationType)
			{
				dynamic animation;
				animationType = animationType.ToLower();

				switch (animationType)
				{
					case "animate":
						animation = new Animate();
						animation.Element = tokeniser.Read();
						animation.Property = tokeniser.Read();
						animation.Value = tokeniser.Read();
						SetInterpolator(animation);
						animation.Delay = tokeniser.Read();
						animation.Duration = tokeniser.Read();
						break;
					case "runevent":
						animation = new RunEvent();
						animation.Event = tokeniser.Read();
						animation.Delay = tokeniser.Read();
						break;
					case "stopevent":
						animation = new StopEvent();
						animation.Event = tokeniser.Read();
						animation.Delay = tokeniser.Read();
						break;
					case "setvisible":
						animation = new SetVisible();
						animation.Element = tokeniser.Read();
						string visible = tokeniser.Read();
						switch (visible)
						{
							case "0":
								animation.Visible = false;
								break;
							case "1":
								animation.Visible = true;
								break;
							default:
								throw new VDFSyntaxException(visible, tokeniser.Position, tokeniser.Line, tokeniser.Character, "\"0\" or \"1\"");
						}
						animation.Delay = tokeniser.Read();
						break;
					case "firecommand":
						animation = new FireCommand();
						animation.Delay = tokeniser.Read();
						animation.Command = tokeniser.Read();
						break;
					case "runeventchild":
						animation = new RunEventChild();
						animation.Element = tokeniser.Read();
						animation.Event = tokeniser.Read();
						animation.Delay = tokeniser.Read();
						break;
					case "setinputenabled":
						animation = new SetInputEnabled();
						animation.Element = tokeniser.Read();
						string enabled = tokeniser.Read();
						switch (enabled)
						{
							case "0":
								animation.Enabled = false;
								break;
							case "1":
								animation.Enabled = true;
								break;
							default:
								throw new VDFSyntaxException(enabled, tokeniser.Position, tokeniser.Line, tokeniser.Character, "\"0\" or \"1\"");
						}
						animation.Delay = tokeniser.Read();
						break;
					case "playsound":
						animation = new PlaySound();
						animation.Delay = tokeniser.Read();
						animation.Sound = tokeniser.Read();
						break;
					case "stoppanelanimations":
						animation = new StopPanelAnimations();
						animation.Element = tokeniser.Read();
						animation.Delay = tokeniser.Read();
						break;
					default:
						throw new VDFSyntaxException(animationType, tokeniser.Position, tokeniser.Line, tokeniser.Character);
				}

				if (tokeniser.Read(true).StartsWith('['))
				{
					animation.OSTag = tokeniser.Read();
				}

				return animation;
			}

			return ParseFile();
		}

		public static string Stringify(Dictionary<string, List<HUDAnimation>> animations)
		{
			const char tab = '\t';
			const string newLine = "\r\n";

			return animations.Aggregate("", (string a, KeyValuePair<string, List<HUDAnimation>> _event) => a + $"event {_event.Key}{newLine}{{{newLine}{_event.Value.Aggregate("", (string a, HUDAnimation animation) => a + $"{tab}{animation}{newLine}")}}}{newLine}");
		}
	}
}
