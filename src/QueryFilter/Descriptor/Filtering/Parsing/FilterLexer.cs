// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html
// All other rights reserved.

namespace QueryFilter
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lexer")]
    public class FilterLexer
    {
        private const char Separator = '~';

        private static readonly string[] ComparisonOperators = new[] { "eq", "ne", "lt", "le", "gt", "ge" };
        private static readonly string[] LogicalOperators = new[] { "and", "or", "not" };
        private static readonly string[] Booleans = new[] { "true", "false" };
        private static readonly string[] Functions = new[] { "contains", "endswith", "startswith", "in", "necontains", "notin", "notendswith", "notstartswith" };

        private int _currentCharacterIndex;
        private readonly string input;

        public FilterLexer(string input)
        {
            input ??= string.Empty;

            this.input = input.Trim(new[] { Separator });
        }

        public IList<FilterToken> Tokenize()
        {
            var tokens = new List<FilterToken>();

            while (_currentCharacterIndex < input.Length)
            {
                if (TryParseIdentifier(out var result))
                {
                    tokens.Add(Identifier(result));
                }
                else if (TryParseNumber(out result))
                {
                    tokens.Add(Number(result));
                }
                else if (TryParseString(out result))
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        tokens.Add(String(result));
                    }
                    else
                    {
                        tokens.Add(NullValue(result));
                    }
                }
                else if (TryParseCharacter(out result, '('))
                {
                    tokens.Add(LeftParenthesis(result));
                }
                else if (TryParseCharacter(out result, ')'))
                {
                    tokens.Add(RightParenthesis(result));
                }
                else if (TryParseCharacter(out result, '['))
                {
                    tokens.Add(LeftSquareBracket(result));
                }
                else if (TryParseCharacter(out result, ']'))
                {
                    tokens.Add(RightSquareBracket(result));
                }
                else if (TryParseCharacter(out result, ','))
                {
                    tokens.Add(Comma(result));
                }
                else
                {
                    throw new FilterParserException("Expected token");
                }
            }

            return tokens;
        }

        private static bool IsComparisonOperator(string value) => Array.IndexOf(ComparisonOperators, value) > -1;

        private static bool IsLogicalOperator(string value) => Array.IndexOf(LogicalOperators, value) > -1;

        private static bool IsBoolean(string value) => Array.IndexOf(Booleans, value) > -1;

        private static bool IsNull(string value) => value.Equals("null", StringComparison.InvariantCultureIgnoreCase);

        private static bool IsEmpty(string value) => value.Equals("empty", StringComparison.InvariantCultureIgnoreCase);

        private static bool IsFunction(string value) => Array.IndexOf(Functions, value) > -1;

        private static FilterToken Comma(string result) => new FilterToken { TokenType = FilterTokenType.Comma, Value = result };

        private static FilterToken Boolean(string result) => new FilterToken { TokenType = FilterTokenType.Boolean, Value = result };

        private static FilterToken NullValue(string result) => new FilterToken { TokenType = FilterTokenType.Null, Value = result };

        private static FilterToken EmptyValue(string result) => new FilterToken { TokenType = FilterTokenType.Empty, Value = result };

        private static FilterToken RightParenthesis(string result) => new FilterToken { TokenType = FilterTokenType.RightParenthesis, Value = result };

        private static FilterToken LeftParenthesis(string result) => new FilterToken { TokenType = FilterTokenType.LeftParenthesis, Value = result };

        private static FilterToken LeftSquareBracket(string result) => new FilterToken { TokenType = FilterTokenType.LeftSquareBracket, Value = result };

        private static FilterToken RightSquareBracket(string result) => new FilterToken { TokenType = FilterTokenType.RightSquareBracket, Value = result };

        private static FilterToken String(string result)
        {
            result = result.Replace("\"", "'");
            return new FilterToken { TokenType = FilterTokenType.String, Value = result };
        }

        private static FilterToken Number(string result) => new FilterToken { TokenType = FilterTokenType.Number, Value = result };

        private FilterToken Date(string result)
        {
            TryParseString(out result);

            return new FilterToken { TokenType = FilterTokenType.DateTime, Value = result };
        }

        private FilterToken Time(string result)
        {
            TryParseString(out result);

            return new FilterToken { TokenType = FilterTokenType.Time, Value = result };
        }

        private FilterToken IgnoreCaseString(string result)
        {
            TryParseString(out result);

            return new FilterToken { TokenType = FilterTokenType.StringUseIgnoreCase, Value = result };
        }

        private static FilterToken ComparisonOperator(string result) => new FilterToken { TokenType = FilterTokenType.ComparisonOperator, Value = result };

        private static FilterToken LogicalOperator(string result)
        {
            if (result == "or")
            {
                return new FilterToken { TokenType = FilterTokenType.Or, Value = result };
            }

            if (result == "and")
            {
                return new FilterToken { TokenType = FilterTokenType.And, Value = result };
            }

            return new FilterToken { TokenType = FilterTokenType.Not, Value = result };
        }

        private static FilterToken Function(string result) => new FilterToken { TokenType = FilterTokenType.Function, Value = result };

        private static FilterToken Property(string result) => new FilterToken { TokenType = FilterTokenType.Property, Value = result };

        private FilterToken Identifier(string result)
        {
            if (result == "datetime")
            {
                return Date(result);
            }
            if (result == "time")
            {
                return Time(result);
            }

            if (result == "ic")
            {
                return IgnoreCaseString(result);
            }

            if (IsComparisonOperator(result))
            {
                return ComparisonOperator(result);
            }

            if (IsLogicalOperator(result))
            {
                return LogicalOperator(result);
            }

            if (IsBoolean(result))
            {
                return Boolean(result);
            }

            if (IsNull(result))
            {
                return NullValue(result);
            }
            if (IsEmpty(result))
            {
                return EmptyValue(result);
            }
            if (IsFunction(result))
            {
                return Function(result);
            }

            return Property(result);
        }

        private void SkipSeparators()
        {
            char currentCharacter = Peek();

            while (currentCharacter == Separator)
            {
                currentCharacter = Next();
            }
        }

        private bool TryParseCharacter(out string character, char whatCharacter)
        {
            SkipSeparators();

            char currentCharacter = Peek();

            if (currentCharacter != whatCharacter)
            {
                character = null;
                return false;
            }

            Next();
            character = currentCharacter.ToString();

            return true;
        }

        private bool TryParseString(out string @string)
        {
            SkipSeparators();

            char currentCharacter = Peek();

            if (currentCharacter != '\'')
            {
                @string = null;
                return false;
            }

            currentCharacter = Next();

            StringBuilder result = new StringBuilder();

            @string = Read(
                character =>
                {
                    if (character == char.MaxValue)
                    {
                        throw new FilterParserException("Unterminated string");
                    }

                    if (character == '\'' && Peek(1) == '\'')
                    {
                        Next();
                        return true;
                    }

                    return character != '\'';
                },
            result);

            Next();

            return true;
        }

        private bool TryParseNumber(out string number)
        {
            SkipSeparators();

            char currentCharacter = Peek();
            StringBuilder result = new StringBuilder();

            int decimalSymbols = 0;

            if (currentCharacter == '-' || currentCharacter == '+')
            {
                result.Append(currentCharacter);
                currentCharacter = Next();
            }

            if (currentCharacter == '.')
            {
                decimalSymbols++;
                result.Append(currentCharacter);
                currentCharacter = Next();
            }

            if (!char.IsDigit(currentCharacter))
            {
                number = null;
                return false;
            }

            number = Read(
                character =>
                {
                    if (character == '.')
                    {
                        if (decimalSymbols < 1)
                        {
                            decimalSymbols++;
                            return true;
                        }

                        throw new FilterParserException("A number cannot have more than one decimal symbol");
                    }

                    return char.IsDigit(character);
                },
            result);
            return true;
        }

        private bool TryParseIdentifier(out string identifier)
        {
            SkipSeparators();

            char currentCharacter = Peek();
            StringBuilder result = new StringBuilder();

            if (!IsIdentifierStart(currentCharacter))
            {
                identifier = null;
                return false;
            }
            else
            {
                result.Append(currentCharacter);
                Next();
            }

            identifier = Read(character => IsIdentifierPart(character) || character == '.', result);

            return true;
        }

        private static bool IsIdentifierPart(char character) => char.IsLetter(character) || char.IsDigit(character) || character == '_' || character == '$';

        private static bool IsIdentifierStart(char character) => char.IsLetter(character) || character == '_' || character == '$' || character == '@';

        private string Read(Func<char, bool> predicate, StringBuilder result)
        {
            char currentCharacter = Peek();

            while (predicate(currentCharacter))
            {
                result.Append(currentCharacter);
                currentCharacter = Next();
            }

            return result.ToString();
        }

        private char Peek() => Peek(0);

        private char Peek(int chars)
        {
            if (_currentCharacterIndex + chars < input.Length)
            {
                return input[_currentCharacterIndex + chars];
            }

            return Char.MaxValue;
        }

        private char Next()
        {
            _currentCharacterIndex++;
            return Peek();
        }
    }
}
