using System;
using System.Linq;
using System.Collections.Generic;

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

namespace HUDMerger
{
	public class HUDAnimation
	{
		public string Type { get; set; }
		public string OSTag { get; set; }
	}

	class Animate : HUDAnimation
	{
		public string Element { get; set; }
		public string Property { get; set; }
		public string Value { get; set; }
		public string Interpolator { get; set; }
		public string Frequency { get; set; }
		public string Bias { get; set; }
		public string Delay { get; set; }
		public string Duration { get; set; }
	}

	class RunEvent : HUDAnimation
	{
		public string Event { get; set; }
		public string Delay { get; set; }
	}

	class StopEvent : HUDAnimation
	{
		public string Event { get; set; }
		public string Delay { get; set; }
	}

	class SetVisible : HUDAnimation
	{
		public string Element { get; set; }
		public string Delay { get; set; }
		public string Duration { get; set; }
	}

	class FireCommand : HUDAnimation
	{
		public string Delay { get; set; }
		public string Command { get; set; }
	}

	class RunEventChild : HUDAnimation
	{
		public string Element { get; set; }
		public string Event { get; set; }
		public string Delay { get; set; }
	}

	class SetInputEnabled : HUDAnimation
	{
		public string Element { get; set; }
		public int Visible { get; set; }
		public string Delay { get; set; }
	}

	class PlaySound : HUDAnimation
	{
		public string Delay { get; set; }
		public string Sound { get; set; }
	}

	class StopPanelAnimations : HUDAnimation
	{
		public string Element { get; set; }
		public string Delay { get; set; }
	}

	public static class HUDAnimations
	{
		public static Dictionary<string, List<HUDAnimation>> Parse(string str)
		{
			int i = 0;
			char[] whiteSpaceIgnore = new char[] { ' ', '\t', '\r', '\n' };

			string Next(bool lookAhead = false)
			{
				string currentToken = "";
				int j = i;

				if (j >= str.Length - 1)
				{
					return "EOF";
				}

				while ((whiteSpaceIgnore.Contains(str[j]) || str[j] == '/') && j < str.Length - 1)
				{
					if (str[j] == '/')
					{
						if (str[j + 1] == '/')
						{
							while (str[j] != '\n' && j < str.Length - 1)
							{
								j++;
							}
						}
					}
					else
					{
						j++;
					}
					if (j >= str.Length)
					{
						return "EOF";
					}
				}

				if (str[j] == '"')
				{
					// Read until next quote (ignore opening quote)
					j++;
					while (str[j] != '"' && j < str.Length - 1)
					{
						if (str[j] == '\n')
						{
							throw new Exception($"Unexpected end of line at position {j}");
						}
						currentToken += str[j];
						j++;
					}
					j++; // Skip over closing quote
				}
				else
				{
					// Read until whitespace (or end of file)
					while (j < str.Length && !whiteSpaceIgnore.Contains(str[j]))
					{
						if (str[j] == '"')
						{
							throw new Exception($"Unexpected double quote at position {j}");
						}
						currentToken += str[j];
						j++;
					}
				}

				if (!lookAhead)
				{
					i = j;
				}

				//if (j > Str.Length)
				//{
				//	return "EOF";
				//}

				return currentToken;
			}

			Dictionary<string, List<HUDAnimation>> ParseFile()
			{
				Dictionary<string, List<HUDAnimation>> animations = new();

				string currentToken = Next();

				// System.Diagnostics.Debugger.Break();

				while (currentToken == "event")
				{
					string eventName = Next();
					animations[eventName] = ParseEvent();
					currentToken = Next();
				}

				return animations;
			}

			List<HUDAnimation> ParseEvent()
			{
				List<HUDAnimation> @event = new();
				string nextToken = Next();
				if (nextToken == "{")
				{
					// string NextToken = Next();
					while (nextToken != "}" && nextToken != "EOF")
					{
						// NextToken is not a closing brace therefore it is the animation type
						// Pass the animation type to the animation
						nextToken = Next();
						if (nextToken != "}")
						{
							@event.Add(ParseAnimation(nextToken));
						}
					}
				}
				else
				{
					throw new Exception($"Unexpected {nextToken} at position {i}! Are you missing an opening brace?");
				}
				return @event;
			}

			void SetInterpolator(Animate animation)
			{
				string interpolator = Next().ToLower();
				if (interpolator == "pulse")
				{
					animation.Interpolator = interpolator;
					animation.Frequency = Next();
				}
				else if (new string[] { "gain", "bias" }.Contains(interpolator))
				{
					animation.Interpolator = interpolator[0].ToString().ToUpper() + interpolator.Substring(1, interpolator.Length - 1);
					animation.Bias = Next();
				}
				else
				{
					animation.Interpolator = interpolator;
				}
			}


			HUDAnimation ParseAnimation(string animationType)
			{
				dynamic animation;
				animationType = animationType.ToLower();

				if (animationType == "animate")
				{
					animation = new Animate();
					animation.Type = animationType;
					animation.Element = Next();
					animation.Property = Next();
					animation.Value = Next();
					SetInterpolator(animation);
					animation.Delay = Next();
					animation.Duration = Next();
				}
				else if (animationType == "runevent")
				{
					animation = new RunEvent();
					animation.Type = animationType;
					animation.Event = Next();
					animation.Delay = Next();
				}
				else if (animationType == "stopevent")
				{
					animation = new StopEvent();
					animation.Type = animationType;
					animation.Event = Next();
					animation.Delay = Next();
				}
				else if (animationType == "setvisible")
				{
					animation = new SetVisible();
					animation.Type = animationType;
					animation.Element = Next();
					animation.Delay = Next();
					animation.Duration = Next();
				}
				else if (animationType == "firecommand")
				{
					animation = new FireCommand();
					animation.Type = animationType;
					animation.Delay = Next();
					animation.Command = Next();
				}
				else if (animationType == "runeventchild")
				{
					animation = new RunEventChild();
					animation.Type = animationType;
					animation.Element = Next();
					animation.Event = Next();
					animation.Delay = Next();
				}
				else if (animationType == "setinputenabled")
				{
					animation = new SetInputEnabled();
					animation.Element = Next();
					animation.Visible = int.Parse(Next());
					animation.Delay = Next();
				}
				else if (animationType == "playsound")
				{
					animation = new PlaySound();
					animation.Delay = Next();
					animation.Sound = Next();
				}
				else if (animationType == "stoppanelanimations")
				{
					animation = new StopPanelAnimations();
					animation.Element = Next();
					animation.Delay = Next();
				}
				else
				{
					System.Diagnostics.Debug.WriteLine(str.Substring(i - 25, 25));
					throw new Exception($"Unexpected {animationType} at position {i}");
				}

				if (Next(true).StartsWith('['))
				{
					animation.OSTag = Next();
				}

				return animation;
			}

			return ParseFile();
		}

		public static string Stringify(Dictionary<string, List<HUDAnimation>> animations)
		{
			string str = "";
			char tab = '\t';
			string newLine = "\r\n";

			string FormatWhiteSpace(string str)
			{
				return System.Text.RegularExpressions.Regex.IsMatch(str, "\\s") ? $"\"{str}\"" : str;
			}

			string GetInterpolator(Animate animation)
			{
				string interpolator = animation.Interpolator.ToLower();
				switch (interpolator)
				{
					case "Pulse":
						return $"Pulse {animation.Frequency}";
					case "Gain":
					case "Bias":
						return $"Gain {animation.Bias}";
					default:
						return $"{animation.Interpolator}";
				}
			}

			foreach (string @event in animations.Keys)
			{
				str += $"event {@event}{newLine}{{{newLine}";
				foreach (dynamic execution in animations[@event])
				{
					str += tab;
					Type T = execution.GetType();
					if (T == typeof(Animate))
					{
						str += $"Animate {FormatWhiteSpace(execution.Element)} {FormatWhiteSpace(execution.Property)} {FormatWhiteSpace(execution.Value)} {GetInterpolator(execution)} {execution.Delay} {execution.Duration}";
					}
					else if (T == typeof(RunEvent))
					{
						str += $"RunEvent {FormatWhiteSpace(execution.Event)} {execution.Delay}";
					}
					else if (T == typeof(StopEvent))
					{
						str += $"StopEvent {FormatWhiteSpace(execution.Event)} {execution.Delay}";
					}
					else if (T == typeof(SetVisible))
					{
						str += $"SetVisible {FormatWhiteSpace(execution.Element)} {execution.Delay} {execution.Duration}";
					}
					else if (T == typeof(FireCommand))
					{
						str += $"FireCommand {execution.Delay} {FormatWhiteSpace(execution.Command)}";
					}
					else if (T == typeof(RunEventChild))
					{
						str += $"RunEventChild {FormatWhiteSpace(execution.Element)} {FormatWhiteSpace(execution.Event)} {execution.Delay}";
					}
					else if (T == typeof(SetVisible))
					{
						str += $"SetVisible {FormatWhiteSpace(execution.Element)} {execution.Visible} {execution.Delay}";
					}
					else if (T == typeof(PlaySound))
					{
						str += $"PlaySound {execution.Delay} {FormatWhiteSpace(execution.Sound)}";
					}

					if (execution.OSTag != null)
					{
						str += " " + execution.OSTag;
					}

					str += newLine;
				}
				str += $"}}{newLine}";
			}

			return str;
		}
	}
}