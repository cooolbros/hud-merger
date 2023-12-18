using System;
using System.Collections.Generic;
using System.Text;
using HUDAnimations.Models;
using HUDAnimations.Models.Animations;
using HUDAnimations.Models.Interpolators;
using VDF;
using VDF.Exceptions;
using VDF.Models;

namespace HUDAnimations;

public static class HUDAnimationsSerializer
{
	public static HUDAnimationsFile Deserialize(string str)
	{
		VDFTokeniser tokeniser = new(str);

		static HUDAnimationsFile ReadFile(VDFTokeniser tokeniser)
		{
			HUDAnimationsFile hudAnimations = [];

			while (true)
			{
				switch (tokeniser.Read())
				{
					case VDFToken { Type: VDFTokenType.String } token when StringComparer.OrdinalIgnoreCase.Equals(token.Value, "event"):
						hudAnimations.Add(ReadEvent(tokeniser));
						break;
					case null:
						return hudAnimations;
					case VDFToken token:
						throw new VDFSyntaxException(
							token,
							["'event'"],
							tokeniser.Index,
							tokeniser.Line,
							tokeniser.Character
						);
				}
			}
		}

		static KeyValue ReadEvent(VDFTokeniser tokeniser)
		{
			string eventName = tokeniser.Read() switch
			{
				VDFToken { Type: VDFTokenType.String } token => token.Value,
				var token => throw new VDFSyntaxException(
					token,
					["event name"],
					tokeniser.Index,
					tokeniser.Line,
					tokeniser.Character
				)
			};

			string? conditional = tokeniser.Read(true) switch
			{
				VDFToken { Type: VDFTokenType.Conditional } => tokeniser.Read(false)!.Value.Value,
				_ => null,
			};

			VDFToken? openingBraceToken = tokeniser.Read();
			if (openingBraceToken is not { Type: VDFTokenType.ControlCharacter, Value: "{" })
			{
				throw new VDFSyntaxException(
					openingBraceToken,
					conditional == null ? ["'{'", "conditional"] : ["'{'"],
					tokeniser.Index,
					tokeniser.Line,
					tokeniser.Character
				);
			}

			List<HUDAnimationBase> animations = [];

			while (true)
			{
				switch (tokeniser.Read())
				{
					case VDFToken { Type: VDFTokenType.String } token:
						animations.Add(ReadAnimation(tokeniser, token.Value));
						break;
					case VDFToken { Type: VDFTokenType.ControlCharacter, Value: "}" }:
						return new KeyValue
						{
							Key = eventName,
							Value = animations,
							Conditional = conditional
						};
					case null:
						throw new VDFSyntaxException(
						null,
						["command", "'}'"],
						tokeniser.Index,
						tokeniser.Line,
						tokeniser.Character
					);
				}
			}
		}

		static HUDAnimationBase ReadAnimation(VDFTokeniser tokeniser, string command)
		{
			string ReadString()
			{
				VDFToken? token = tokeniser.Read();
				return token is { Type: VDFTokenType.String }
					? token.Value.Value
					: throw new VDFSyntaxException(
						token,
						["value"],
						tokeniser.Index,
						tokeniser.Line,
						tokeniser.Character
					);
			}

			float ReadNumber()
			{
				// Support floats with multiple decimals e.g. "0.1.5"
				string value = "";
				bool seen = false;
				foreach (char c in ReadString())
				{
					if (c == '.')
					{
						if (!seen)
						{
							value += c;
							seen = true;
						}
					}
					else
					{
						value += c;
					}
				}

				return float.Parse(value);
			}

			bool ReadBool()
			{
				return ReadString() switch
				{
					"1" => true,
					"0" => false,
					var str => throw new VDFSyntaxException(
						new VDFToken { Type = VDFTokenType.String, Value = str },
						["'1'", "'0'"],
						tokeniser.Index,
						tokeniser.Line,
						tokeniser.Character
					)
				};
			}

			string? ReadConditional()
			{
				return tokeniser.Read(true) is { Type: VDFTokenType.Conditional }
					? tokeniser.Read(false)!.Value.Value
					: null;
			}

			return command.ToLower() switch
			{
				"animate" => new Animate
				{
					Element = ReadString(),
					Property = ReadString(),
					Value = ReadString(),
					Interpolator = ReadString().ToLower() switch
					{
						"accel" => new AccelInterpolator(),
						"bias" => new BiasInterpolator { Bias = ReadNumber() },
						"bounce" => new BounceInterpolator(),
						"deaccel" => new DeAccelInterpolator(),
						"flicker" => new FlickerInterpolator { Randomness = ReadNumber() },
						"gain" => new GainInterpolator { Bias = ReadNumber() },
						"linear" => new LinearInterpolator(),
						"pulse" => new PulseInterpolator { Frequency = ReadNumber() },
						"spline" => new SplineInterpolator(),
						string interpolator => throw new VDFSyntaxException(
							new VDFToken { Type = VDFTokenType.String, Value = interpolator },
							["Accel", "Bias", "Bounce", "DeAccel", "Flicker", "Gain", "Linear", "Pulse", "Spline"],
							tokeniser.Index,
							tokeniser.Line,
							tokeniser.Character
						)
					},
					Delay = ReadNumber(),
					Duration = ReadNumber(),
					Conditional = ReadConditional()
				},
				"runevent" => new RunEvent
				{
					Event = ReadString(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"stopevent" => new StopEvent
				{
					Event = ReadString(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"setvisible" => new SetVisible
				{
					Element = ReadString(),
					Visible = ReadBool(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"firecommand" => new FireCommand
				{
					Delay = ReadNumber(),
					Command = ReadString(),
					Conditional = ReadConditional()
				},
				"runeventchild" => new RunEventChild
				{
					Element = ReadString(),
					Event = ReadString(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"setinputenabled" => new SetInputEnabled
				{
					Element = ReadString(),
					Enabled = ReadBool(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"playsound" => new PlaySound
				{
					Delay = ReadNumber(),
					Sound = ReadString(),
					Conditional = ReadConditional()
				},
				"stoppanelanimations" => new StopPanelAnimations
				{
					Element = ReadString(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				"stopanimation" => new StopAnimation
				{
					Element = ReadString(),
					Property = ReadString(),
					Delay = ReadNumber(),
					Conditional = ReadConditional()
				},
				string str => throw new VDFSyntaxException(
					new VDFToken { Type = VDFTokenType.String, Value = str },
					["animate"],
					tokeniser.Index,
					tokeniser.Line,
					tokeniser.Character
				)
			};
		}

		return ReadFile(tokeniser);
	}

	public static string Serialize(HUDAnimationsFile hudAnimations)
	{
		const char tab = '\t';
		const string newLine = "\r\n";

		string str = "";

		foreach (KeyValue keyValue in hudAnimations)
		{
			str += $"event {keyValue.Key}" + (keyValue.Conditional != null ? $" {keyValue.Conditional}" : "") + newLine;
			str += $"{{" + newLine;

			switch (keyValue.Value)
			{
				case List<HUDAnimationBase> animations:
					foreach (HUDAnimationBase animation in animations)
					{
						str += $"{tab}{animation}{newLine}";
					}
					break;
				default:
					throw new NotSupportedException();
			}

			str += $"}}" + newLine;
		}

		return str;
	}
}
