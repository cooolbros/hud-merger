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

				while ((whiteSpaceIgnore.Contains(str[j]) || str[j] == '/') && j <= str.Length - 1)
				{
					if (str[j] == '/')
					{
						if (str[j + 1] == '/')
						{
							// prevent index out of bounds error if file doesn't end in a new line
							while (j < str.Length && str[j] != '\n')
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
					while (str[j] != '"' && j < str.Length)
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
					while (!whiteSpaceIgnore.Contains(str[j]) && j < str.Length - 1)
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

				return currentToken;
			}

			Dictionary<string, dynamic> ParseObject(bool isobject = false)
			{
				Dictionary<string, dynamic> obj = new();

				string currentToken = Next();
				string nextToken = Next(true);

				while (currentToken != "}" && nextToken != "EOF")
				{
					if (nextToken.StartsWith('[') && osTags)
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
						if (Next(true).StartsWith('[') && osTags)
						{
							currentToken += $"{OSTagDelimeter}{Next()}";
						}

						if (obj.ContainsKey(currentToken))
						{
							// dynamic property exists
							if (obj[currentToken].GetType() == typeof(List<dynamic>))
							{
								// Array already exists

								if (nextToken == "{" || nextToken == "}")
								{
									throw new Exception($"Cannot set value of {currentToken} to {nextToken}! Are you missing an opening brace?");
								}

								if (nextToken == "EOF")
								{
									throw new Exception($"Cannot set value of {currentToken} to EOF, expected value!");
								}

								obj[currentToken].Add(nextToken);
							}
							else
							{
								// Primitive type already exists
								dynamic value = obj[currentToken];

								if (nextToken == "{" || nextToken == "}")
								{
									throw new Exception($"Cannot set value of {currentToken} to {nextToken}!Are you missing an opening brace ?");
								}

								if (nextToken == "EOF")
								{
									throw new Exception($"Cannot set value of ${currentToken} to EOF, expected value!");
								}

								obj[currentToken] = new List<dynamic>();
								obj[currentToken].Add(value);
								obj[currentToken].Add(nextToken);
							}
						}
						else
						{
							// Property doesn't exist
							if (currentToken == "{" || currentToken == "}")
							{
								throw new Exception($"Cannot create property {currentToken}, Are you mising an opening brace?");
							}
							if (nextToken == "EOF")
							{
								throw new Exception($"Unexpected end of line, expected value for {currentToken}");
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
							throw new Exception("Unexpected '}'! Are you missing an opening brace?");
						}
					}
				}

				return obj;
			}

			return ParseObject();
		}

		public static string Stringify(Dictionary<string, dynamic> obj, int tabs = 0)
		{
			string str = "";
			char space = ' ';
			string newLine = "\r\n";

			int longestKeyLength = 0;

			foreach (string key in obj.Keys)
			{
				if (obj[key].GetType() != typeof(Dictionary<string, dynamic>))
				{
					longestKeyLength = Math.Max(longestKeyLength, key.Split(VDF.OSTagDelimeter)[0].Length);
				}
			}

			longestKeyLength += 4;

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
								str += $"{new String(space, tabs * 4)}\"{keyTokens[0]}\" {keyTokens[1]}{newLine}";
							}
							else
							{
								// No OS Tag
								str += $"{new String(space, tabs * 4)}\"{key}\"{newLine}";
							}
							str += $"{new String(space, tabs * 4)}{{{newLine}";
							str += $"{VDF.Stringify(item, tabs + 1)}{new String(space, tabs * 4)}}}{newLine}";
						}
						else
						{
							if (keyTokens.Length > 1)
							{
								// OS Tag
								str += $"{new String(space, tabs * 4)}\"{keyTokens[0]}\"{new String(space, longestKeyLength - keyTokens[0].Length)}\"{item}\" {keyTokens[1]}{newLine}";
							}
							else
							{
								// No OS Tag
								str += $"{new String(space, tabs * 4)}\"{key}\"{new String(space, longestKeyLength - key.Length)}\"{item}\"{newLine}";
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
							str += $"{new String(space, tabs * 4)}\"{keyTokens[0]}\" {keyTokens[1]}{newLine}";
							str += $"{new String(space, tabs * 4)}{{{newLine}";
							str += $"{VDF.Stringify(obj[key], tabs + 1)}{new String(space, tabs * 4)}}}{newLine}";
						}
						else
						{
							// No OS Tag
							str += $"{new String(space, tabs * 4)}\"{key}\"{newLine}";
							str += $"{new String(space, tabs * 4)}{{{newLine}";
							str += $"{VDF.Stringify(obj[key], tabs + 1)}{new String(space, tabs * 4)}}}{newLine}";
						}
					}
					else
					{
						if (keyTokens.Length > 1)
						{
							// OS Tag
							str += $"{new String(space, tabs * 4)}\"{keyTokens[0]}\"{new String(space, longestKeyLength - keyTokens[0].Length)}\"{obj[key]}\" {keyTokens[1]}{newLine}";
						}
						else
						{
							// No OS Tag
							str += $"{new String(space, tabs * 4)}\"{key}\"{new String(space, longestKeyLength - key.Length)}\"{obj[key]}\"{newLine}";
						}
					}
				}
			}

			return str;
		}
	}
}
