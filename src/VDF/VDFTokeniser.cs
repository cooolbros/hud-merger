using System;
using System.IO;
using System.Linq;
using VDF.Exceptions;
using VDF.Models;

namespace VDF;

public class VDFTokeniser(string str)
{
	private static readonly char[] WhiteSpaceIgnore = [' ', '\t', '\r', '\n'];
	private static readonly char[] WhiteSpaceTokenTerminate = ['"', '{', '}'];

	private readonly string Str = str;
	public int Index { get; private set; } = 0;
	public int Line { get; private set; } = 0;
	public int Character { get; private set; } = 0;
	private bool EOFRead = false;

	public VDFToken? Read(bool peek = false)
	{
		int index = Index;
		int line = Line;
		int character = Character;

		while (index < Str.Length && (WhiteSpaceIgnore.Contains(Str[index]) || Str[index] == '/'))
		{
			if (Str[index] == '\n')
			{
				line++;
				character = 0;
			}
			else if (Str[index] == '/')
			{
				int index1 = index + 1;
				if (index1 < Str.Length && Str[index1] == '/')
				{
					index++;
					while (index < Str.Length && Str[index] != '\n')
					{
						index++;
					}
					line++;
					character = 0;
				}
				else
				{
					break;
				}
			}
			else
			{
				character++;
			}
			index++;
		}

		if (index >= Str.Length)
		{
			if (EOFRead)
			{
				throw new EndOfStreamException();
			}
			if (!peek)
			{
				EOFRead = true;
			}
			return null;
		}

		VDFToken token;

		switch (Str[index])
		{
			case '{':
			case '}':
				{
					token = new VDFToken
					{
						Type = VDFTokenType.ControlCharacter,
						Value = Str[index].ToString(),
					};

					index++;
					character++;
					break;
				}
			case '"':
				{
					index++;
					character++;
					int start = index;

					while (Str[index] != '"')
					{
						if (index >= Str.Length)
						{
							throw new VDFSyntaxException(
								new VDFToken { Type = VDFTokenType.String, Value = "EOF" },
								["char", "\""],
								index,
								line,
								character
							);
						}

						if (Str[index] == '\\')
						{
							index++;
							character++;

							if (index >= Str.Length)
							{
								throw new VDFSyntaxException(
									new VDFToken { Type = VDFTokenType.String, Value = "EOF" },
									["char"],
									index,
									line,
									character
								);
							}
						}
						else if (Str[index] == '\n')
						{
							throw new VDFSyntaxException(
								new VDFToken { Type = VDFTokenType.String, Value = "\\n" },
								["\""],
								index,
								line,
								character
							);
						}

						index++;
						character++;
					}

					int end = index;
					index++;
					character++;

					token = new VDFToken
					{
						Type = VDFTokenType.String,
						Value = Str[start..end],
					};

					break;
				}
			default:
				{
					int start = index;

					while (index < Str.Length)
					{
						if (WhiteSpaceIgnore.Contains(Str[index]) || WhiteSpaceTokenTerminate.Contains(Str[index]))
						{
							break;
						}

						if (Str[index] == '\\')
						{
							index++;
							character++;

							if (index >= Str.Length)
							{
								throw new VDFSyntaxException(
									new VDFToken { Type = VDFTokenType.String, Value = "EOF" },
									["char"],
									index,
									line,
									character
								);
							}
						}

						index++;
						character++;
					}

					int end = index;

					string str = Str[start..end];

					token = new VDFToken
					{
						Type = str.StartsWith('[') && str.EndsWith(']') ? VDFTokenType.Conditional : VDFTokenType.String,
						Value = str,
					};

					break;
				}
		}

		if (!peek)
		{
			Index = index;
			Line = line;
			Character = character;
		}

		return token;
	}
}
