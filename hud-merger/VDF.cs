using System;
using System.Linq;
using System.Collections.Generic;

namespace hud_merger
{
	static class VDF
	{
		public static Dictionary<string, dynamic> Parse(string Str, string OSTagDelimeter = "%")
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

			Dictionary<string, dynamic> ParseObject()
			{
				Dictionary<string, dynamic> Obj = new();

				string CurrentToken = Next();
				string NextToken = Next(true);

				while (CurrentToken != "}" && NextToken != "EOF")
				{
					if (Next(true).StartsWith('['))
					{
						// Object with OS Tag
						CurrentToken += $"{OSTagDelimeter}{Next()}";
						Next(); // Skip over opening brace
						Obj[CurrentToken] = ParseObject();
					}
					else if (NextToken == "{")
					{
						// Object
						Next(); // Skip over opening brace

						if (Obj.TryGetValue(CurrentToken, out dynamic Value))
						{
							if (Obj[CurrentToken].GetType() == typeof(List<dynamic>))
							{
								// Object list exists
								Obj[CurrentToken].Add(ParseObject());
							}
							else
							{
								// Object already exists
								Obj[CurrentToken] = new List<dynamic>();
								Obj[CurrentToken].Add(Value);
								Obj[CurrentToken].Add(ParseObject());
							}
						}
						else
						{
							// Object doesnt exist
							Obj[CurrentToken] = ParseObject();
						}
					}
					else
					{
						// Primitive

						Next(); // Skip over value

						// Check primitive os tag
						if (Next(true).StartsWith('['))
						{
							CurrentToken += $"{OSTagDelimeter}{Next()}";
						}

						if (Obj.TryGetValue(CurrentToken, out dynamic Value))
						{
							// dynamic property exists
							if (Obj[CurrentToken].GetType() == typeof(List<dynamic>))
							{
								// Array already exists
								Obj[CurrentToken].Add(NextToken);
							}
							else
							{
								// Primitive type already exists
								Obj[CurrentToken] = new List<dynamic>();
								Obj[CurrentToken].Add(Value);
								Obj[CurrentToken].Add(NextToken);
							}
						}
						else
						{
							// Property doesn't exist
							Obj[CurrentToken] = NextToken;
						}
					}

					CurrentToken = Next();
					NextToken = Next(true);
				}

				return Obj;
			}

			return ParseObject();
		}

		public static string Stringify(Dictionary<string, dynamic> Obj, int Tabs = 0)
		{
			string Str = "";
			char Tab = '\t';
			string NewLine = "\r\n";
			foreach (string Key in Obj.Keys)
			{
				if (Obj[Key].GetType() == typeof(List<dynamic>))
				{
					// Item has multiple instances
					foreach (dynamic Item in Obj[Key])
					{
						if (Item.GetType() == typeof(Dictionary<string, dynamic>))
						{
							string[] KeyTokens = Key.Split('%');
							if (KeyTokens.Length > 1)
							{
								// OS Tag
								Str += $"{new String(Tab, Tabs)}\"{Key}\" {KeyTokens[1]}{NewLine}";
							}
							else
							{
								// No OS Tag
								Str += $"{new String(Tab, Tabs)}{Key}{NewLine}";
							}
							Str += $"{new String(Tab, Tabs)}{{{NewLine}";
							Str += $"{VDF.Stringify(Item, Tabs + 1)}{new String(Tab, Tabs)}}}{NewLine}";
						}
						else
						{
							string[] KeyTokens = Key.Split('%');
							if (KeyTokens.Length > 1)
							{
								// OS Tag
								Str += $"{new String(Tab, Tabs)}\"{Key}\"\t\"{Item}\" {KeyTokens[1]}{NewLine}";
							}
							else
							{
								// No OS Tag
								Str += $"{new String(Tab, Tabs)}\"{Key}\"\t\"{Item}\"{NewLine}";
							}
						}
					}
				}
				else
				{
					// There is only one object object/value
					if (Obj[Key] is IDictionary<string, dynamic>)
					{
						string[] KeyTokens = Key.Split('%');
						if (KeyTokens.Length > 1)
						{
							Str += $"{new String(Tab, Tabs)}\"{KeyTokens[0]}\" {KeyTokens[1]}{NewLine}";
							Str += $"{new String(Tab, Tabs)}{{{NewLine}";
							Str += $"{VDF.Stringify(Obj[Key], Tabs + 1)}{new String(Tab, Tabs)}}}{NewLine}";
						}
						else
						{
							// No OS Tag
							Str += $"{new String(Tab, Tabs)}\"{Key}\"{NewLine}";
							Str += $"{new String(Tab, Tabs)}{{{NewLine}";
							Str += $"{VDF.Stringify(Obj[Key], Tabs + 1)}{new String(Tab, Tabs)}}}{NewLine}";

						}
					}
					else
					{
						string[] KeyTokens = Key.Split('%');
						if (KeyTokens.Length > 1)
						{
							// OS Tag
							Str += $"{new String(Tab, Tabs)}\"{KeyTokens[0]}\"\t\"{Obj[Key]}\" {KeyTokens[1]}{NewLine}";
						}
						else
						{
							// No OS Tag
							Str += $"{new String(Tab, Tabs)}\"{Key}\"\t\"{Obj[Key]}\"{NewLine}";
						}
					}
				}

			}
			return Str;
		}

	}
}
