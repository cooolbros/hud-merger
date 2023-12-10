using System;
using System.Collections.Generic;
using VDF.Exceptions;
using VDF.Models;

namespace VDF;

public static class VDFSerializer
{
	public static KeyValues Deserialize(string str)
	{
		VDFTokeniser tokeniser = new(str);

		static KeyValues ReadKeyValues(VDFTokeniser tokeniser, bool isObject)
		{
			KeyValues keyValues = [];

			while (true)
			{
				KeyValue? keyValue = tokeniser.Read() switch
				{
					{ Type: VDFTokenType.ControlCharacter, Value: "}" } when isObject => null,
					null when !isObject => null,
					null => throw new VDFSyntaxException(
						null,
						["key"],
						tokeniser.Index,
						tokeniser.Line,
						tokeniser.Character
					),
					{ Type: VDFTokenType.String } token => ReadKeyValue(tokeniser, token.Value),
					VDFToken token => throw new VDFSyntaxException(
						token,
						["key"],
						tokeniser.Index,
						tokeniser.Line,
						tokeniser.Character
					),
				};

				if (keyValue == null)
				{
					break;
				}

				keyValues.Add(keyValue.Value);
			}

			return keyValues;
		}

		static KeyValue ReadKeyValue(VDFTokeniser tokeniser, string key)
		{
			VDFToken? valueToken = tokeniser.Read();

			string? conditional;

			if (valueToken is { Type: VDFTokenType.Conditional })
			{
				conditional = valueToken.Value.Value;
				valueToken = tokeniser.Read();
			}
			else
			{
				conditional = null;
			}

			return valueToken switch
			{
				{ Type: VDFTokenType.ControlCharacter, Value: "{" } => new KeyValue
				{
					Key = key,
					Value = ReadKeyValues(tokeniser, true),
					Conditional = conditional,
				},
				{ Type: VDFTokenType.String } token => new KeyValue
				{
					Key = key,
					Value = token.Value,
					Conditional = ReadConditional(tokeniser) ?? conditional,
				},
				VDFToken token => throw new VDFSyntaxException(
					token,
					["value", "{"],
					tokeniser.Index,
					tokeniser.Line,
					tokeniser.Character
				),
				null => throw new VDFSyntaxException(
					null,
					["value", "{"],
					tokeniser.Index,
					tokeniser.Line,
					tokeniser.Character
				),
			};
		}

		static string? ReadConditional(VDFTokeniser tokeniser)
		{
			if (tokeniser.Read(true) is { Type: VDFTokenType.Conditional } token)
			{
				tokeniser.Read();
				return token.Value;
			}

			return null;
		}

		return ReadKeyValues(tokeniser, false);
	}

	public static string Serialize(IEnumerable<KeyValue> keyValues, int level = 0)
	{
		string str = "";

		foreach (KeyValue keyValue in keyValues)
		{
			if (keyValue.Value is IEnumerable<KeyValue> kvs)
			{
				str += $"{new string('\t', level)}\"{keyValue.Key}\"{(keyValue.Conditional != null ? $" {keyValue.Conditional}" : "")}\r\n";
				str += $"{new string('\t', level)}{{\r\n";
				str += $"{Serialize(kvs, level + 1)}{new string('\t', level)}}}\r\n";
			}
			else if (keyValue.Value is string value)
			{
				str += $"{new string('\t', level)}\"{keyValue.Key}\"\t\"{value}\"{(keyValue.Conditional != null ? $" {keyValue.Conditional}" : "")}\r\n";
			}
#if DEBUG
			else
			{
				throw new ArgumentException("keyValue.Value");
			}
#endif
		}

		return str;
	}
}
