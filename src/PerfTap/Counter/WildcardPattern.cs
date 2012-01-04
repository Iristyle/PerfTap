namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using NLog;

	public class WildcardPattern
	{
		private const char _escapeChar = '`';
		private Lazy<Regex> _patternRegex = new Lazy<Regex>();
		private const string _regexChars = @"()[].?*{}^$+|\";
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		internal WildcardOptions Options { get; private set; }
		internal string Pattern { get; private set; }

		public WildcardPattern(string pattern, WildcardOptions options)
		{
			if (pattern == null)
			{
				throw new ArgumentNullException("pattern");
			}
			this.Pattern = pattern;
			this.Options = options;
			_patternRegex = new Lazy<Regex>(() => Build(this.Pattern, this.Options));
		}

		private static Regex Build(string pattern, WildcardOptions options)
		{
			RegexOptions regexOptions = RegexOptions.Singleline;
			if (IsOptionSet(options, WildcardOptions.Compiled))
			{
				regexOptions |= RegexOptions.Compiled;
			}
			if (IsOptionSet(options, WildcardOptions.IgnoreCase))
			{
				regexOptions |= RegexOptions.IgnoreCase;
			}
			if ((options & WildcardOptions.CultureInvariant) == WildcardOptions.CultureInvariant)
			{
				regexOptions |= RegexOptions.CultureInvariant;
			}
			try
			{
				return new Regex(ConvertWildcardToRegex(pattern), regexOptions);
			}
			catch (ArgumentException ex)
			{
				throw new ArgumentException(string.Format(GetEventResources.InvalidPattern, pattern), ex);
			}
		}

		/*
		private static string ConvertWildcardToRegex(string pattern)
		{
			if (pattern == null)
			{
				return null;
			}
			
			if (pattern.Length == 0)
			{
				return "^$";
			}

			string str = pattern;
			char[] chArray = new char[(str.Length * 2) + 2];
			int length = 0;
			bool flag = false;
			bool isEscaped = false;
			int num2 = 0;
			char ch = str[0];
			if (ch != '*')
			{
				chArray[length++] = '^';
				switch (ch)
				{
					case _escapeChar:
						isEscaped = true;
						break;

					case '?':
						chArray[length++] = '.';
						break;

					default:
						if (IsNonWildcardRegexChar(ch))
						{
							chArray[length++] = '\\';
							chArray[length++] = ch;
						}
						else
						{
							chArray[length++] = ch;
							if (ch == '[')
							{
								flag = true;
							}
						}
						break;
				}
				if (str.Length == 1)
				{
					chArray[length++] = '$';
				}
			}
			num2 = 1;
			while (num2 < (str.Length - 1))
			{
				ch = str[num2];
				if (IsNonWildcardRegexChar(ch))
				{
					chArray[length++] = '\\';
				}
				switch (ch)
				{
					case '[':
					case ']':
						if (!isEscaped)
						{
							break;
						}
						chArray[length++] = '\\';
						goto Label_013F;

					case '?':
						if (isEscaped)
						{
							chArray[length++] = '\\';
						}
						else if (!flag)
						{
							ch = '.';
						}
						goto Label_013F;

					case '*':
						if (isEscaped)
						{
							chArray[length++] = '\\';
						}
						else if (!flag)
						{
							chArray[length++] = '.';
						}
						goto Label_013F;

					default:
						goto Label_013F;
				}
				if (ch == '[')
				{
					flag = true;
				}
				else
				{
					flag = false;
				}
			Label_013F:
				if (!isEscaped || (ch != '`'))
				{
					isEscaped = ch == _escapeChar;
				}
				else
				{
					isEscaped = false;
				}
				if (!isEscaped)
				{
					chArray[length++] = ch;
				}
				num2++;
			}
			if (num2 < str.Length)
			{
				ch = str[num2];
				if (isEscaped)
				{
					if (IsWildcardChar(ch))
					{
						chArray[length++] = '\\';
					}
				}
				else if (ch == '?')
				{
					ch = '.';
				}
				else if (IsNonWildcardRegexChar(ch))
				{
					chArray[length++] = '\\';
				}
				if ((ch != '*') || isEscaped)
				{
					chArray[length++] = ch;
					chArray[length++] = '$';
				}
			}
			str = new string(chArray, 0, length);
			_log.Info(() => string.Format("Converted Wildcard ({0}) to Regex ({1})", pattern, str));
			return str;
		}
		*/

		private static string ConvertWildcardToRegex(string pattern)
		{
			if (pattern == null) { return null; }
			if (pattern.Length == 0) { return "^$"; }

			char[] convertedChars = new char[(pattern.Length * 2) + 2];

			int length = 0;
			bool flag1 = false;
			bool isEscaped = false;
			char firstCharacter = pattern[0];
			if (firstCharacter != '*')
			{
				convertedChars[length++] = '^';
				if (firstCharacter == _escapeChar)
					isEscaped = true;
				else if (firstCharacter == '?')
					convertedChars[length++] = '.';
				else if (IsNonWildcardRegexChar(firstCharacter))
				{
					char[] chArray2 = convertedChars;
					int index1 = length;
					int num1 = 1;
					int num2 = index1 + num1;
					chArray2[index1] = '\\';
					char[] chArray3 = convertedChars;
					int index2 = num2;
					int num4 = 1;
					length = index2 + num4;
					int num5 = firstCharacter;
					chArray3[index2] = (char)num5;
				}
				else
				{
					convertedChars[length++] = firstCharacter;
					if (firstCharacter == '[')
						flag1 = true;
				}
				if (pattern.Length == 1)
					convertedChars[length++] = '$';
			}

			int index3;
			for (index3 = 1; index3 < pattern.Length - 1; ++index3)
			{
				char ch2 = pattern[index3];
				if (IsNonWildcardRegexChar(ch2))
					convertedChars[length++] = '\\';
				switch (ch2)
				{
					case '*':
						if (isEscaped) { convertedChars[length++] = '\\'; }
						else if (!flag1) { convertedChars[length++] = '.'; }

						break;
					case '?':
						if (isEscaped) { convertedChars[length++] = '\\'; }
						else if (!flag1) { ch2 = '.'; }

						break;

					case '[':
					case ']':
						if (isEscaped) { convertedChars[length++] = '\\'; }
						else { flag1 = ch2 == '['; }

						break;
				}

				isEscaped = (!isEscaped || ch2 != _escapeChar) && ch2 == _escapeChar;
				if (!isEscaped)
					convertedChars[length++] = ch2;
			}
			if (index3 < pattern.Length)
			{
				char ch2 = pattern[index3];
				if (isEscaped)
				{
					if (IsWildcardChar(ch2))
						convertedChars[length++] = '\\';
				}
				else if (ch2 == '?')
					ch2 = '.';
				else if (IsNonWildcardRegexChar(ch2))
					convertedChars[length++] = '\\';
				if (ch2 != '*' || isEscaped)
				{
					char[] chArray2 = convertedChars;
					int index1 = length;
					int num1 = 1;
					int num2 = index1 + num1;
					int num3 = ch2;
					chArray2[index1] = (char)num3;
					char[] chArray3 = convertedChars;
					int index2 = num2;
					int num4 = 1;
					length = index2 + num4;
					int num5 = '$';
					chArray3[index2] = (char)num5;
				}
			}
			string materialized = new string(convertedChars, 0, length);
			_log.Info(() => string.Format("Converted Wildcard ({0}) to Regex ({1})", pattern, materialized));
			return materialized;
		}

		private static bool IsOptionSet(WildcardOptions input, WildcardOptions options)
		{
			return (input & options) != WildcardOptions.None;
		}

		public bool IsMatch(string input)
		{
			return this._patternRegex.Value.IsMatch(input);
		}

		private static bool IsNonWildcardRegexChar(char ch)
		{
			return (!IsWildcardChar(ch) && IsRegexChar(ch));
		}

		private static bool IsRegexChar(char ch)
		{
			return _regexChars.Contains(ch);
		}

		private static bool IsWildcardChar(char ch)
		{
			if (((ch != '*') && (ch != '?')) && (ch != '['))
			{
				return (ch == ']');
			}

			return true;
		}
	}
}