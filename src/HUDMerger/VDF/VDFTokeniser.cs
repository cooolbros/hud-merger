using System;
using System.IO;
using System.Linq;
using HUDMergerVDF.Exceptions;
using HUDMergerVDF.Models;

namespace HUDMergerVDF
{
	public class VDFTokeniser
	{
		private static readonly char[] WhiteSpaceIgnore = { ' ', '\t', '\r', '\n' };

		private static readonly char[] WhiteSpaceTokenTerminate = { '"', '{', '}' };

		private string Str;

		public VDFParseOptions Options { get; }

		public int Position { get; private set; }
		public int Line { get; private set; }
		public int Character { get; private set; }
		public byte Quoted { get; private set; }

		// Peek
		private string _peekToken;
		private int _peekPosition;
		private int _peekLine;
		private int _peekCharacter;
		private byte _peekQuoted;

		// EOFRead
		private bool EOFRead;

		public VDFTokeniser(string str, VDFParseOptions options = default)
		{
			Str = str;
			Options = options ?? new VDFParseOptions();
			Position = 0;
			Line = 0;
			Character = 0;
			Quoted = 0;
			EOFRead = false;
		}

		public string Read(bool peek = false)
		{
			string currentToken = "";

			if (_peekToken != null)
			{
				currentToken = _peekToken;
				Position = _peekPosition;
				Line = _peekLine;
				Character = _peekCharacter;
				Quoted = _peekQuoted;
				_peekToken = null;
				return currentToken;
			}

			int i = Position;
			int line = Line;
			int character = Character;
			byte quoted = Quoted;

			if (i >= Str.Length)
			{
				if (!peek)
				{
					if (EOFRead)
					{
						throw new EndOfStreamException();
					}
					EOFRead = true;
				}
				return "EOF";
			}

			while (i < Str.Length && (VDFTokeniser.WhiteSpaceIgnore.Contains(Str[i]) || Str[i] == '/'))
			{
				if (Str[i] == '\n')
				{
					line++;
					character = 0;
				}
				else if (Str[i] == '/')
				{
					int i1 = i + 1;
					if (i1 < Str.Length && Str[i1] == '/')
					{
						i++;
						while (i < Str.Length && Str[i] != '\n')
						{
							i++;
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

				i++;
			}

			if (i >= Str.Length)
			{
				if (!peek)
				{
					if (EOFRead)
					{
						throw new EndOfStreamException();
					}
					EOFRead = true;
				}
				return "EOF";
			}

			if (Str[i] == '"')
			{
				i++;
				character++;
				quoted = 1;

				while (Str[i] != '"')
				{
					if (i >= Str.Length)
					{
						throw new VDFSyntaxException("EOF", i, line, character, "\"");
					}

					if (Str[i] == '\n')
					{
						if (!Options.AllowMultilineStrings)
						{
							throw new VDFSyntaxException("\n", i, line, character, "\"");
						}
						line++;
					}

					if (Str[i] == '\\')
					{
						currentToken += '\\';
						i++;
						character++;

						if (i >= Str.Length)
						{
							throw new VDFSyntaxException("\\", i, line, character);
						}

						currentToken += Str[i];
						i++;
						character++;
					}
					else
					{
						currentToken += Str[i];
						i++;
						character++;
					}
				}

				i++;
				character++;
			}
			else
			{
				quoted = 0;
				while (i < Str.Length && !VDFTokeniser.WhiteSpaceIgnore.Contains(Str[i]))
				{
					if (Str[i] == '\\')
					{
						currentToken += '\\';
						i++;
						character++;

						if (i >= Str.Length)
						{
							throw new VDFSyntaxException("\\", i, line, character);
						}

						currentToken += Str[i];
						i++;
						character++;
					}
					else if (VDFTokeniser.WhiteSpaceTokenTerminate.Contains(Str[i]))
					{
						if (currentToken == "")
						{
							// VDFTokeniser.WhiteSpaceTokenTerminate contains a '"' but it that should not be
							// the case here because if currentToken is "" it would be a quoted token
							currentToken += Str[i];
							i++;
							character++;
						}
						break;
					}
					else
					{
						currentToken += Str[i];
						i++;
						character++;
					}
				}
			}

			if (peek)
			{
				_peekToken = currentToken;
				_peekPosition = i;
				_peekLine = line;
				_peekCharacter = character;
				_peekQuoted = quoted;
			}
			else
			{
				Position = i;
				Line = line;
				Character = character;
				Quoted = quoted;
			}

			return currentToken;
		}
	}
}
