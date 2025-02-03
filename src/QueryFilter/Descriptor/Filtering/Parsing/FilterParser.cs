
// This source is subject to the GNU General Public License, version 3
// See https://www.gnu.org/licenses/quick-guide-gplv3.html 
// All other rights reserved.

namespace QueryFilter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class FilterParser
    {
        private readonly IList<FilterToken> _tokens;
        private int _currentTokenIndex;

        public FilterParser(string input)
        {
            var lexer = new FilterLexer(input);
            _tokens = lexer.Tokenize();
        }

        public IFilterNode Parse()
        {
            if (_tokens.Count > 0)
            {
                return Expression();
            }

            return null;
        }

        private IFilterNode Expression() => OrExpression();

        private IFilterNode OrExpression()
        {
            var firstArgument = AndExpression();

            if (Is(FilterTokenType.Or))
            {
                return ParseOrExpression(firstArgument);
            }

            if (Is(FilterTokenType.And))
            {
                Expect(FilterTokenType.And);

                return new AndNode
                {
                    First = firstArgument,
                    Second = OrExpression()
                };
            }

            return firstArgument;
        }

        private IFilterNode AndExpression()
        {
            var firstArgument = ComparisonExpression();

            if (Is(FilterTokenType.And))
            {
                return ParseAndExpression(firstArgument);
            }

            return firstArgument;
        }

        private IFilterNode ComparisonExpression()
        {
            var firstArgument = PrimaryExpression();

            if (Is(FilterTokenType.ComparisonOperator) || Is(FilterTokenType.Function))
            {
                return ParseComparisonExpression(firstArgument);
            }

            return firstArgument;
        }

        private IFilterNode PrimaryExpression()
        {
            if (Is(FilterTokenType.LeftParenthesis))
            {
                return ParseNestedExpression();
            }

            if (Is(FilterTokenType.Function))
            {
                return ParseFunctionExpression();
            }

            if (Is(FilterTokenType.Boolean))
            {
                return ParseBoolean();
            }

            if (Is(FilterTokenType.Null))
            {
                return ParseNull();
            }

            if (Is(FilterTokenType.Empty))
            {
                return ParseEmpty();
            }

            if (Is(FilterTokenType.DateTime))
            {
                return ParseDateTimeExpression();
            }
            if (Is(FilterTokenType.Time))
            {
                return ParseTimeExpression();
            }

            if (Is(FilterTokenType.Property))
            {
                return ParsePropertyExpression();
            }

            if (Is(FilterTokenType.Number))
            {
                return ParseNumberExpression();
            }

            if (Is(FilterTokenType.String))
            {
                return ParseStringExpression();
            }
            if (Is(FilterTokenType.StringUseIgnoreCase))
            {
                return ParseStringExpressionUseIgnoreCase();
            }
            if (Is(FilterTokenType.LeftSquareBracket))
            {
                return ParseArrayExpression();
            }
            throw new FilterParserException("Expected primaryExpression");
        }

        private IFilterNode ParseOrExpression(IFilterNode firstArgument)
        {
            Expect(FilterTokenType.Or);
            var secondArgument = OrExpression();
            return new OrNode
            {
                First = firstArgument,
                Second = secondArgument
            };
        }

        private IFilterNode ParseComparisonExpression(IFilterNode firstArgument)
        {
            if (Is(FilterTokenType.ComparisonOperator))
            {
                var comparison = Expect(FilterTokenType.ComparisonOperator);

                var secondArgument = PrimaryExpression();

                return new ComparisonNode
                {
                    First = firstArgument,
                    FilterOperator = comparison.ToFilterOperator(),
                    Second = secondArgument
                };
            }

            var function = Expect(FilterTokenType.Function);

            var functionNode = new FunctionNode
            {
                FilterOperator = function.ToFilterOperator()
            };

            functionNode.Arguments.Add(firstArgument);
            functionNode.Arguments.Add(PrimaryExpression());

            return functionNode;
        }

        private IFilterNode ParseAndExpression(IFilterNode firstArgument)
        {
            Expect(FilterTokenType.And);
            var secondArgument = ComparisonExpression();
            return new AndNode
            {
                First = firstArgument,
                Second = secondArgument
            };
        }

        private IFilterNode ParseStringExpression()
        {
            var stringToken = Expect(FilterTokenType.String);

            return new StringNode
            {
                Value = stringToken.Value
            };
        }
        private IFilterNode ParseStringExpressionUseIgnoreCase()
        {
            var stringToken = Expect(FilterTokenType.StringUseIgnoreCase);

            return new StringNode
            {
                Value = stringToken.Value,
                IgnoreCaseSensitive = true
            };
        }

        private IFilterNode ParseArrayExpression()
        {
            var stringToken = Expect(FilterTokenType.LeftSquareBracket);

            var list = new List<object>
            {
                Expression()
            };

            while (Is(FilterTokenType.Comma))
            {
                Expect(FilterTokenType.Comma);
                list.Add(Expression());
            }

            Expect(FilterTokenType.RightSquareBracket);

            return new ArrayNode
            {
                Value = list.ToArray()
            };

        }

        private IFilterNode ParseBoolean()
        {
            var stringToken = Expect(FilterTokenType.Boolean);

            return new BooleanNode
            {
                Value = Convert.ToBoolean(stringToken.Value)
            };
        }

        private IFilterNode ParseNull()
        {
            var stringToken = Expect(FilterTokenType.Null);

            return new BooleanNode
            {
                Value = null
            };
        }

        private IFilterNode ParseEmpty()
        {
            Expect(FilterTokenType.Empty);

            return new StringNode
            {
                Value = string.Empty
            };
        }

        private IFilterNode ParseNumberExpression()
        {
            var number = Expect(FilterTokenType.Number);

            return new NumberNode
            {
                Value = Convert.ToDouble(number.Value, CultureInfo.InvariantCulture)
            };
        }

        private IFilterNode ParsePropertyExpression()
        {
            var property = Expect(FilterTokenType.Property);

            return new PropertyNode
            {
                Name = property.Value
            };
        }

        private IFilterNode ParseDateTimeExpression()
        {
            var dateTime = Expect(FilterTokenType.DateTime);
            var acceptDates = new string[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };
            return new DateTimeNode
            {
                Value = DateTime.ParseExact(dateTime.Value, acceptDates, null, DateTimeStyles.None)
            };
        }
        private IFilterNode ParseTimeExpression()
        {
            var dateTime = Expect(FilterTokenType.Time);
            var acceptDates = new string[] { "h\\:mm", "g" };
            return new TimeNode
            {
                Value = TimeSpan.ParseExact(dateTime.Value, acceptDates, null,TimeSpanStyles.AssumeNegative)
            };
        }
        private IFilterNode ParseNestedExpression()
        {
            Expect(FilterTokenType.LeftParenthesis);
            var expression = Expression();
            if (expression is ILogicalNode)
                (expression as ILogicalNode).IsNested = true;
            Expect(FilterTokenType.RightParenthesis);
            return expression;
        }

        private IFilterNode ParseFunctionExpression()
        {
            var function = Expect(FilterTokenType.Function);

            var functionNode = new FunctionNode
            {
                FilterOperator = function.ToFilterOperator()
            };

            Expect(FilterTokenType.LeftSquareBracket);
            functionNode.Arguments.Add(Expression());

            /*while (Is(FilterTokenType.Comma))
            {
                Expect(FilterTokenType.Comma);
                functionNode.Arguments.Add(Expression());
            }
            Expect(FilterTokenType.RightSquareBracket); */

            return functionNode;
        }

        private FilterToken Expect(FilterTokenType tokenType)
        {
            if (!Is(tokenType))
            {
                throw new FilterParserException("Expected " + tokenType);
            }

            var token = Peek();
            _currentTokenIndex++;
            return token;
        }

        private bool Is(FilterTokenType tokenType)
        {
            var token = Peek();
            return token != null && token.TokenType == tokenType;
        }

        private FilterToken Peek()
        {
            if (_currentTokenIndex < _tokens.Count)
            {
                return _tokens[_currentTokenIndex];
            }

            return null;
        }
    }
}
