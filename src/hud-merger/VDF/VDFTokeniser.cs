using System;
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

		public VDFTokeniser(string str, VDFParseOptions options = default)
		{
			this.Str = str;
			this.Options = options ?? new VDFParseOptions();
			this.Position = 0;
			this.Line = 0;
			this.Character = 0;
			this.Quoted = 0;
		}

		public string Read(bool peek = false)
		{
			string currentToken = "";

			if (this._peekToken != null)
			{
				currentToken = this._peekToken;
				this.Position = this._peekPosition;
				this.Line = this._peekLine;
				this.Character = this._peekCharacter;
				this.Quoted = this._peekQuoted;
				this._peekToken = null;
				return currentToken;
			}

			int i = this.Position;
			int line = this.Line;
			int character = this.Character;
			byte quoted = this.Quoted;

			if (i >= this.Str.Length)
			{
				return "EOF";
			}

			while (i < this.Str.Length && (VDFTokeniser.WhiteSpaceIgnore.Contains(this.Str[i]) || this.Str[i] == '/'))
			{
				if (this.Str[i] == '\n')
				{
					line++;
					character = 0;
				}
				else if (this.Str[i] == '/')
				{
					int i1 = i + 1;
					if (i1 < this.Str.Length && this.Str[i1] == '/')
					{
						i++;
						while (i < this.Str.Length && this.Str[i] != '\n')
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

			if (i >= this.Str.Length)
			{
				return "EOF";
			}

			if (this.Str[i] == '"')
			{
				i++;
				character++;
				quoted = 1;

				while (this.Str[i] != '"')
				{
					if (i >= this.Str.Length)
					{
						throw new VDFSyntaxException("EOF", i, line, character, "\"");
					}

					if (this.Str[i] == '\n')
					{
						if (!this.Options.AllowMultilineStrings)
						{
							throw new VDFSyntaxException("\n", i, line, character, "\"");
						}
						line++;
					}

					if (this.Str[i] == '\\')
					{
						currentToken += '\\';
						i++;
						character++;

						if (i >= this.Str.Length)
						{
							throw new VDFSyntaxException("\\", i, line, character);
						}

						currentToken += this.Str[i];
						i++;
						character++;
					}
					else
					{
						currentToken += this.Str[i];
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
				while (i < this.Str.Length && !VDFTokeniser.WhiteSpaceIgnore.Contains(this.Str[i]))
				{
					if (this.Str[i] == '\\')
					{
						currentToken += '\\';
						i++;
						character++;

						if (i >= this.Str.Length)
						{
							throw new VDFSyntaxException("\\", i, line, character);
						}

						currentToken += this.Str[i];
						i++;
						character++;
					}
					else if (VDFTokeniser.WhiteSpaceTokenTerminate.Contains(this.Str[i]))
					{
						if (currentToken == "")
						{
							// VDFTokeniser.WhiteSpaceTokenTerminate contains a '"' but it that should not be
							// the case here because if currentToken is "" it would be a quoted token
							currentToken += this.Str[i];
							i++;
							character++;
						}
						break;
					}
					else
					{
						currentToken += this.Str[i];
						i++;
						character++;
					}
				}
			}

			if (peek)
			{
				this._peekToken = currentToken;
				this._peekPosition = i;
				this._peekLine = line;
				this._peekCharacter = character;
				this._peekQuoted = quoted;
			}
			else
			{
				this.Position = i;
				this.Line = line;
				this.Character = character;
				this.Quoted = quoted;
			}

			return currentToken;
		}
	}
}
