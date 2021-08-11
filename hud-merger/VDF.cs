using System;
using System.Collections.Generic;
using System.Linq;

namespace hud_merger
{
	static class VDF
	{
		public static char OSTagDelimeter = '^';

		public static Dictionary<string, dynamic> Parse(string str, bool osTags = true)
		{
			char[] whiteSpaceIgnore = new char[] { ' ', '\t', '\r', '\n' };

			int i = 0;
			int line = 1;
			int pos = 1;

			string Next(bool lookAhead = false)
			{
				string currentToken = "";
				int j = i;

				int _line = line;
				int _pos = pos;

				if (j >= str.Length - 1)
				{
					return "EOF";
				}

				while ((whiteSpaceIgnore.Contains(str[j]) || str[j] == '/') && j <= str.Length - 1)
				{
					if (str[j] == '\n')
					{
						_line++;
						_pos = 0;
					}
					else
					{
						_pos++;
					}

					if (str[j] == '/')
					{
						if (str[j + 1] == '/')
						{
							// prevent index out of bounds error if file doesn't end in a new line
							while (j < str.Length && str[j] != '\n')
							{
								j++;
							}
							_line++;
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
					while (str[j] != '"' && j < str.Length)
					{
						if (str[j] == '\n')
						{
							throw new Exception($"Unexpected EOL at position {j} (line {line}, position {pos})! Are you missing a closing \"?");
						}
						currentToken += str[j];
						j++;
					}
					j++; // Skip over closing quote
				}
				else
				{
					// Read until whitespace (or end of file)
					while (!whiteSpaceIgnore.Contains(str[j]) && j < str.Length - 1)
					{
						if (str[j] == '"')
						{
							throw new Exception($"Unexpected '\"' at position {j} (line {line}, position {pos})! Are you missing terminating whitespace?");
						}
						currentToken += str[j];
						j++;
					}
				}

				if (!lookAhead)
				{
					i = j;
					line = _line;
					pos = _pos;
				}

				return currentToken;
			}

			Dictionary<string, dynamic> ParseObject(bool isobject = false)
			{
				Dictionary<string, dynamic> obj = new();

				string currentToken = Next();
				string nextToken = Next(true);

				while (currentToken != "}" && nextToken != "EOF")
				{
					if (nextToken.StartsWith('[') && nextToken.EndsWith(']') && osTags)
					{
						// Object with OS Tag
						currentToken += $"{OSTagDelimeter}{Next()}";
						Next(); // Skip over opening brace
						obj[currentToken] = ParseObject(true);
					}
					else if (nextToken == "{")
					{
						// Object
						Next(); // Skip over opening brace

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
								obj[currentToken] = new List<dynamic>();
								obj[currentToken].Add(value);
								obj[currentToken].Add(ParseObject(true));
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

						Next(); // Skip over value

						// Check primitive os tag
						string tokenLookAhead = Next(true);
						if (tokenLookAhead.StartsWith('[') && tokenLookAhead.EndsWith(']') && osTags)
						{
							currentToken += $"{OSTagDelimeter}{Next()}";
						}

						if (obj.ContainsKey(currentToken))
						{
							// dynamic property exists
							if (obj[currentToken].GetType() == typeof(List<dynamic>))
							{
								// Array already exists

								if (nextToken == "}")
								{
									throw new Exception($"Unexpected '}}' at position {i} (line {line}, position {pos})! Are you missing an {{?");
								}

								if (nextToken == "EOF")
								{
									throw new Exception($"Unexpected EOF at position {i} (line {line}, position {pos})! Are you missing a value?");
								}

								obj[currentToken].Add(nextToken);
							}
							else
							{
								// Primitive type already exists
								dynamic value = obj[currentToken];

								if (nextToken == "}")
								{
									throw new Exception($"Unexpected '}}' at position {i} (line {line}, position {pos})! Are you missing an opening brace?");
								}

								if (nextToken == "EOF")
								{
									throw new Exception($"Unexpected EOF at position {i} (line {line}, position {pos})!  Are you missing a value?");
								}

								obj[currentToken] = new List<dynamic>();
								obj[currentToken].Add(value);
								obj[currentToken].Add(nextToken);
							}
						}
						else
						{
							// Property doesn't exist
							if (currentToken == "}")
							{
								// throw new VDFSyntaxError($"Cannot create property {currentToken}");
								throw new Exception($"Cannot create property {currentToken}, Are you mising an opening brace?");
							}
							if (nextToken == "EOF")
							{
								throw new Exception($"Unexpected EOF at position {i} (line {line}, position {pos})! Expected value for {currentToken}");
							}
							obj[currentToken] = nextToken;
						}
					}

					currentToken = Next();
					nextToken = Next(true);

					if (currentToken == "EOF")
					{
						if (isobject)
						{
							// we are expecting a closing brace not EOF, error!
							throw new Exception("Unexpected end of file! Are you missing a closing brace?");
						}
						else
						{
							// we are not inside an object, possibly parsing #base statements, EOF is fine
							break;
						}
					}

					if (!isobject)
					{
						if (currentToken == "}")
						{
							throw new Exception($"Unexpected '}}' position {i} (line {line}, position {pos}) Are you missing an opening brace?");
						}
					}
				}

				return obj;
			}

			return ParseObject();
		}

		public enum IndentationKind
		{
			Tabs,
			Spaces
		}

		public static string Stringify(Dictionary<string, dynamic> obj, IndentationKind indentation = IndentationKind.Tabs)
		{
			const char tab = '\t';
			const char space = ' ';
			const string newLine = "\r\n";

			bool tabIndentation = indentation == IndentationKind.Tabs;
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
