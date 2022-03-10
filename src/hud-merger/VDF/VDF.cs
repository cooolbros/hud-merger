using System;
using System.Collections.Generic;
using System.Linq;
using HUDMergerVDF.Exceptions;
using HUDMergerVDF.Models;

namespace HUDMergerVDF
{
	/// <summary>
	/// Provides functionality to serialize objects or value types to VDF and to deserialize VDF into objects or value types
	/// </summary>
	static class VDF
	{
		/// <summary>
		/// Char in Dictionary key to indicate that the Key/Value has an OS Tag
		/// </summary>
		public static char OSTagDelimeter = '^';

		/// <summary>
		/// Parse VDF into a Dictionary
		/// </summary>
		/// <param name="str">String</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		/// <exception cref="VDFSyntaxException"></exception>
		public static Dictionary<string, dynamic> Parse(string str, VDFParseOptions options = default)
		{
			options ??= new VDFParseOptions();

			VDFTokeniser tokeniser = new VDFTokeniser(str, options);

			Dictionary<string, dynamic> ParseObject(bool isobject = false)
			{
				Dictionary<string, dynamic> obj = new(options.KeyComparer);

				string currentToken = tokeniser.Read();
				string nextToken = tokeniser.Read(true);

				string objectTerminator = isobject ? "}" : "EOF";
				while (currentToken != objectTerminator)
				{
					if (nextToken.StartsWith('[') && nextToken.EndsWith(']') && (tokeniser.Options.OSTags == VDFOSTags.Objects || tokeniser.Options.OSTags == VDFOSTags.All))
					{
						// Object with OS Tag
						currentToken += $"{OSTagDelimeter}{tokeniser.Read()}";
						tokeniser.Read(); // Skip over opening brace
						obj[currentToken] = ParseObject(true);
					}
					else if (nextToken == "{")
					{
						// Object
						tokeniser.Read(); // Skip over opening brace

						if (obj.ContainsKey(currentToken))
						{
							if (obj[currentToken].GetType() == typeof(List<dynamic>))
							{
								// Object list exists
								obj[currentToken].Add(ParseObject(true));
							}
							else
							{
								// Object already exists
								dynamic value = obj[currentToken];
								obj[currentToken] = new List<dynamic>() { value, ParseObject(true) };
							}
						}
						else
						{
							// Object doesnt exist
							obj[currentToken] = ParseObject(true);
						}
					}
					else
					{
						// Primitive

						tokeniser.Read(); // Skip over value

						// Check primitive os tag
						string tokenLookAhead = tokeniser.Read(true);
						if (tokenLookAhead.StartsWith('[') && tokenLookAhead.EndsWith(']') && (tokeniser.Options.OSTags == VDFOSTags.Strings || tokeniser.Options.OSTags == VDFOSTags.All))
						{
							currentToken += $"{OSTagDelimeter}{tokeniser.Read()}";
						}

						if (currentToken == objectTerminator)
						{
							throw new VDFSyntaxException(currentToken, tokeniser.Position, tokeniser.Line, tokeniser.Character);
						}

						if (obj.ContainsKey(currentToken))
						{
							// dynamic property exists
							if (obj[currentToken].GetType() == typeof(List<dynamic>))
							{
								// Array already exists
								obj[currentToken].Add(nextToken);
							}
							else
							{
								// Primitive type already exists
								dynamic value = obj[currentToken];
								obj[currentToken] = new List<dynamic>() { value, nextToken };
							}
						}
						else
						{
							// Property doesn't exist
							obj[currentToken] = nextToken;
						}
					}

					currentToken = tokeniser.Read();
					nextToken = tokeniser.Read(true);
				}

				return obj;
			}

			return ParseObject();
		}

		/// <summary>
		/// Stringify a Dictionary into VDF
		/// </summary>
		/// <param name="obj">Object</param>
		/// <param name="options">Stringify Options</param>
		/// <returns></returns>
		public static string Stringify(Dictionary<string, dynamic> obj, VDFStringifyOptions options = default)
		{
			VDFStringifyOptions _options = options ?? new VDFStringifyOptions();

			const char tab = '\t';
			const char space = ' ';
			const string newLine = "\r\n";

			bool tabIndentation = _options.Indentation == VDFIndentation.Tabs;
			Func<int, string> GetIndentation = tabIndentation ? ((int level) => new String(tab, level)) : ((int level) => new String(space, level * 4));
			Func<int, int, string> GetWhitespace = tabIndentation ? ((int longest, int current) =>
			{
				return new String(tab, ((longest + 2) / 4 - (current + 2) / 4) + 2);
			}) : ((int longest, int current) =>
			{
				int diffResolver = (longest + 2) - (current + 2);
				int nextIndentResolver = 4 - (longest + 2) % 4;
				return new String(space, diffResolver + nextIndentResolver + 4);
			});

			string StringifyObject(Dictionary<string, dynamic> obj, int level = 0)
			{
				string str = "";

				int longestKeyLength = obj.Keys.Aggregate(0, (int total, string current) => Math.Max(total, obj[current].GetType() != typeof(Dictionary<string, dynamic>) ? current.Split(VDF.OSTagDelimeter)[0].Length : 0));

				foreach (string key in obj.Keys)
				{
					string[] keyTokens = key.Split(VDF.OSTagDelimeter);

					if (obj[key].GetType() == typeof(List<dynamic>))
					{
						// Item has multiple instances
						foreach (dynamic item in obj[key])
						{
							if (item.GetType() == typeof(Dictionary<string, dynamic>))
							{
								if (keyTokens.Length > 1)
								{
									// OS Tag
									str += $"{GetIndentation(level)}\"{keyTokens[0]}\" {keyTokens[1]}{newLine}";
								}
								else
								{
									// No OS Tag
									str += $"{GetIndentation(level)}\"{key}\"{newLine}";
								}
								str += $"{GetIndentation(level)}{{{newLine}";
								str += $"{StringifyObject(item, level + 1)}";
								str += $"{GetIndentation(level)}}}{newLine}";
							}
							else
							{
								if (keyTokens.Length > 1)
								{
									// OS Tag
									str += $"{GetIndentation(level)}\"{keyTokens[0]}\"{GetWhitespace(longestKeyLength, keyTokens[0].Length)}\"{item}\" {keyTokens[1]}{newLine}";
								}
								else
								{
									// No OS Tag
									str += $"{GetIndentation(level)}\"{key}\"{GetWhitespace(longestKeyLength, key.Length)}\"{item}\"{newLine}";
								}
							}
						}
					}
					else
					{
						// There is only one object object/value
						if (obj[key].GetType() == typeof(Dictionary<string, dynamic>))
						{
							if (keyTokens.Length > 1)
							{
								// OS Tag
								str += $"{GetIndentation(level)}\"{keyTokens[0]}\" {keyTokens[1]}{newLine}";
							}
							else
							{
								// No OS Tag
								str += $"{GetIndentation(level)}\"{key}\"{newLine}";
							}
							str += $"{GetIndentation(level)}{{{newLine}";
							str += $"{StringifyObject(obj[key], level + 1)}";
							str += $"{GetIndentation(level)}}}{newLine}";
						}
						else
						{
							if (keyTokens.Length > 1)
							{
								// OS Tag
								str += $"{GetIndentation(level)}\"{keyTokens[0]}\"{GetWhitespace(longestKeyLength, keyTokens[0].Length)}\"{obj[key]}\" {keyTokens[1]}{newLine}";
							}
							else
							{
								// No OS Tag
								str += $"{GetIndentation(level)}\"{key}\"{GetWhitespace(longestKeyLength, key.Length)}\"{obj[key]}\"{newLine}";
							}
						}
					}
				}

				return str;
			}

			return StringifyObject(obj);
		}
	}
}
