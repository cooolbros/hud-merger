using System;
using System.Collections.Generic;
using System.Linq;

namespace hud_merger
{
	static class VDF
	{
		public static char OSTagDelimeter = '^';

		public static Dictionary<string, dynamic> Parse(string Str, bool OSTags = true)
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
							// prevent index out of bounds error if file doesn't end in a new line
							while (j < Str.Length && Str[j] != '\n')
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
						// Budhud's gamemenu.res has a multiline value, they are possible but usually syntax errors
						// Assume syntax is correct
						// if (Str[j] == '\n')
						// {
						// 	throw new Exception($"Unexpected end of line at position {j}");
						// }
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

				return CurrentToken;
			}

			Dictionary<string, dynamic> ParseObject(bool IsObject = false)
			{
				Dictionary<string, dynamic> Obj = new();

				string CurrentToken = Next();
				string NextToken = Next(true);

				while (CurrentToken != "}" && NextToken != "EOF")
				{
					if (NextToken.StartsWith('[') && OSTags)
					{
						// Object with OS Tag
						CurrentToken += $"{OSTagDelimeter}{Next()}";
						Next(); // Skip over opening brace
						Obj[CurrentToken] = ParseObject(true);
					}
					else if (NextToken == "{")
					{
						// Object
						Next(); // Skip over opening brace

						if (Obj.ContainsKey(CurrentToken))
						{
							if (Obj[CurrentToken].GetType() == typeof(List<dynamic>))
							{
								// Object list exists
								Obj[CurrentToken].Add(ParseObject(true));
							}
							else
							{
								// Object already exists
								dynamic Value = Obj[CurrentToken];
								Obj[CurrentToken] = new List<dynamic>();
								Obj[CurrentToken].Add(Value);
								Obj[CurrentToken].Add(ParseObject(true));
							}
						}
						else
						{
							// Object doesnt exist
							Obj[CurrentToken] = ParseObject(true);
						}
					}
					else
					{
						// Primitive

						Next(); // Skip over value

						// Check primitive os tag
						if (Next(true).StartsWith('[') && OSTags)
						{
							CurrentToken += $"{OSTagDelimeter}{Next()}";
						}

						if (Obj.ContainsKey(CurrentToken))
						{
							// dynamic property exists
							if (Obj[CurrentToken].GetType() == typeof(List<dynamic>))
							{
								// Array already exists

								if (NextToken == "{" || NextToken == "}")
								{
									throw new Exception($"Cannot set value of {CurrentToken} to {NextToken}! Are you missing an opening brace?");
								}

								if (NextToken == "EOF")
								{
									throw new Exception($"Cannot set value of {CurrentToken} to EOF, expected value!");
								}

								Obj[CurrentToken].Add(NextToken);
							}
							else
							{
								// Primitive type already exists
								dynamic Value = Obj[CurrentToken];

								if (NextToken == "{" || NextToken == "}")
								{
									throw new Exception($"Cannot set value of {CurrentToken} to {NextToken}!Are you missing an opening brace ?");
								}

								if (NextToken == "EOF")
								{
									throw new Exception($"Cannot set value of ${CurrentToken} to EOF, expected value!");
								}

								Obj[CurrentToken] = new List<dynamic>();
								Obj[CurrentToken].Add(Value);
								Obj[CurrentToken].Add(NextToken);
							}
						}
						else
						{
							// Property doesn't exist
							if (CurrentToken == "{" || CurrentToken == "}")
							{
								throw new Exception($"Cannot create property {CurrentToken}, Are you mising an opening brace?");
							}
							if (NextToken == "EOF")
							{
								throw new Exception($"Unexpected end of line, expected value for {CurrentToken}");
							}
							Obj[CurrentToken] = NextToken;
						}
					}

					CurrentToken = Next();
					NextToken = Next(true);

					if (CurrentToken == "EOF")
					{
						if (IsObject)
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

					if (!IsObject)
					{
						if (CurrentToken == "}")
						{
							throw new Exception("Unexpected '}'! Are you missing an opening brace?");
						}
					}
				}

				return Obj;
			}

			return ParseObject();
		}

		public static string Stringify(Dictionary<string, dynamic> Obj, int Tabs = 0)
		{
			string Str = "";
			char Space = ' ';
			string NewLine = "\r\n";

			int LongestKeyLength = 0;

			foreach (string Key in Obj.Keys)
			{
				if (Obj[Key].GetType() != typeof(Dictionary<string, dynamic>))
				{
					LongestKeyLength = Math.Max(LongestKeyLength, Key.Split(VDF.OSTagDelimeter)[0].Length);
				}
			}

			LongestKeyLength += 4;

			foreach (string Key in Obj.Keys)
			{
				string[] KeyTokens = Key.Split(VDF.OSTagDelimeter);

				if (Obj[Key].GetType() == typeof(List<dynamic>))
				{
					// Item has multiple instances
					foreach (dynamic Item in Obj[Key])
					{
						if (Item.GetType() == typeof(Dictionary<string, dynamic>))
						{
							if (KeyTokens.Length > 1)
							{
								// OS Tag
								Str += $"{new String(Space, Tabs * 4)}\"{KeyTokens[0]}\" {KeyTokens[1]}{NewLine}";
							}
							else
							{
								// No OS Tag
								Str += $"{new String(Space, Tabs * 4)}\"{Key}\"{NewLine}";
							}
							Str += $"{new String(Space, Tabs * 4)}{{{NewLine}";
							Str += $"{VDF.Stringify(Item, Tabs + 1)}{new String(Space, Tabs * 4)}}}{NewLine}";
						}
						else
						{
							if (KeyTokens.Length > 1)
							{
								// OS Tag
								Str += $"{new String(Space, Tabs * 4)}\"{KeyTokens[0]}\"{new String(Space, LongestKeyLength - KeyTokens[0].Length)}\"{Item}\" {KeyTokens[1]}{NewLine}";
							}
							else
							{
								// No OS Tag
								Str += $"{new String(Space, Tabs * 4)}\"{Key}\"{new String(Space, LongestKeyLength - Key.Length)}\"{Item}\"{NewLine}";
							}
						}
					}
				}
				else
				{
					// There is only one object object/value
					if (Obj[Key].GetType() == typeof(Dictionary<string, dynamic>))
					{
						if (KeyTokens.Length > 1)
						{
							Str += $"{new String(Space, Tabs * 4)}\"{KeyTokens[0]}\" {KeyTokens[1]}{NewLine}";
							Str += $"{new String(Space, Tabs * 4)}{{{NewLine}";
							Str += $"{VDF.Stringify(Obj[Key], Tabs + 1)}{new String(Space, Tabs * 4)}}}{NewLine}";
						}
						else
						{
							// No OS Tag
							Str += $"{new String(Space, Tabs * 4)}\"{Key}\"{NewLine}";
							Str += $"{new String(Space, Tabs * 4)}{{{NewLine}";
							Str += $"{VDF.Stringify(Obj[Key], Tabs + 1)}{new String(Space, Tabs * 4)}}}{NewLine}";
						}
					}
					else
					{
						if (KeyTokens.Length > 1)
						{
							// OS Tag
							Str += $"{new String(Space, Tabs * 4)}\"{KeyTokens[0]}\"{new String(Space, LongestKeyLength - KeyTokens[0].Length)}\"{Obj[Key]}\" {KeyTokens[1]}{NewLine}";
						}
						else
						{
							// No OS Tag
							Str += $"{new String(Space, Tabs * 4)}\"{Key}\"{new String(Space, LongestKeyLength - Key.Length)}\"{Obj[Key]}\"{NewLine}";
						}
					}
				}
			}

			return Str;
		}
	}
}
