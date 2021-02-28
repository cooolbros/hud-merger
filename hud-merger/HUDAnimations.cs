using System;
using System.Linq;
using System.Collections.Generic;

namespace hud_merger
{
	interface IHUDAnimation { }

	class Animate : IHUDAnimation
	{
		public string Type { get; set; }
		public string Element { get; set; }
		public string Property { get; set; }
		public string Value { get; set; }
		public string Interpolator { get; set; }
		public float Delay { get; set; }
		public float Duration { get; set; }
	}

	class RunEvent : IHUDAnimation
	{
		public string Type { get; set; }
		public string Event { get; set; }
		public float Delay { get; set; }
	}

	class StopEvent : IHUDAnimation
	{
		public string Type { get; set; }
		public string Event { get; set; }
		public float Delay { get; set; }
	}

	static class HUDAnimations
	{
		public static Dictionary<string, List<IHUDAnimation>> Parse(string Str)
		{
			int i = 0;
			char[] WhiteSpaceIgnore = new char[] { ' ', '\t', '\r', '\n' };

			string Next(bool LookAhead = false)
			{
				string CurrentToken = "";
				int j = i;

				if (j >= Str.Length - 1)
				{
					return "EOF";
				}

				while ((WhiteSpaceIgnore.Contains(Str[j]) || Str[j] == '/') && j <= Str.Length - 1)
				{
					if (Str[j] == '/')
					{
						if (Str[j + 1] == '/')
						{
							while (Str[j] != '\n')
							{
								j++;
							}
						}
					}
					else
					{
						j++;
					}
					if (j >= Str.Length)
					{
						return "EOF";
					}
				}

				if (Str[j] == '"')
				{
					// Read until next quote (ignore opening quote)
					j++;
					while (Str[j] != '"' && j < Str.Length)
					{
						if (Str[j] == '\n')
						{
							throw new Exception($"Unexpected end of line at position {j}");
						}
						CurrentToken += Str[j];
						j++;
					}
					j++; // Skip over closing quote
				}
				else
				{
					// Read until whitespace (or end of file)
					while (!WhiteSpaceIgnore.Contains(Str[j]) && j < Str.Length - 1)
					{
						if (Str[j] == '"')
						{
							throw new Exception($"Unexpected double quote at position {j}");
						}
						CurrentToken += Str[j];
						j++;
					}
				}

				if (!LookAhead)
				{
					i = j;
				}

				//if (j > Str.Length)
				//{
				//	return "EOF";
				//}

				return CurrentToken;
			}

			Dictionary<string, List<IHUDAnimation>> ParseFile()
			{
				Dictionary<string, List<IHUDAnimation>> Animations = new();

				string CurrentToken = Next();

				// System.Diagnostics.Debugger.Break();

				while (CurrentToken == "event")
				{
					string EventName = Next();
					Animations[EventName] = ParseEvent();
					CurrentToken = Next();
				}

				return Animations;
			}

			List<IHUDAnimation> ParseEvent()
			{
				List<IHUDAnimation> Event = new();
				string NextToken = Next();
				if (NextToken == "{")
				{
					// string NextToken = Next();
					while (NextToken != "}" && NextToken != "EOF")
					{
						// NextToken is not a closing brace therefore it is the animation type
						// Pass the animation type to the animation
						NextToken = Next();
						if (NextToken != "}")
						{
							Event.Add(ParseAnimation(NextToken));
						}
					}
				}
				else
				{
					throw new Exception($"Unexpected ${NextToken} at position {i}! Are you missing an opening brace?");
				}
				return Event;
			}

			IHUDAnimation ParseAnimation(string AnimationType)
			{
				dynamic Animation;

				if (AnimationType == "Animate")
				{
					Animation = new Animate();
					Animation.Type = AnimationType;
					Animation.Element = Next();
					Animation.Property = Next();
					Animation.Value = Next();
					Animation.Interpolator = Next();
					Animation.Delay = float.Parse(Next());
					Animation.Duration = float.Parse(Next());
				}
				else if (AnimationType == "RunEvent")
				{
					Animation = new RunEvent();
					Animation.Type = AnimationType;
					Animation.Event = Next();
					Animation.Delay = float.Parse(Next());
				}
				else if (AnimationType == "StopEvent")
				{
					Animation = new StopEvent();
					Animation.Type = AnimationType;
					Animation.Event = Next();
					Animation.Delay = float.Parse(Next());
				}
				else
				{
					throw new Exception($"Unexpected {AnimationType} at position {i}");
				}

				return Animation;
			}

			return ParseFile();
		}

		public static string Stringify(Dictionary<string, List<IHUDAnimation>> Animations)
		{
			string Str = "";
			char Tab = '\t';
			string NewLine = "\r\n";

			string FormatWhiteSpace(string Str)
			{
				return System.Text.RegularExpressions.Regex.IsMatch(Str, "\\s") ? $"\"{Str}\"" : Str;
			}

			foreach (string Event in Animations.Keys)
			{
				Str += $"event {Event}{NewLine}{{{NewLine}";
				foreach (dynamic Execution in Animations[Event])
				{
					Str += Tab;
					if (Execution is Animate)
					{
						Str += $"Animate {FormatWhiteSpace(Execution.Element)} {FormatWhiteSpace(Execution.Property)} {FormatWhiteSpace(Execution.Value)} {FormatWhiteSpace(Execution.Interpolator)} {Execution.Delay} {Execution.Duration}";
					}
					else
					{
						Str += $"{FormatWhiteSpace(Execution.Type)} {FormatWhiteSpace(Execution.Event)} {Execution.Delay}";
					}
					Str += NewLine;
				}
				Str += $"}}{NewLine}";
			}

			return Str;
		}
	}
}