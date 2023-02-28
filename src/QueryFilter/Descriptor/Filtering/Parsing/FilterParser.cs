
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
        private readonly IList<FilterToken> tokens;
        private int currentTokenIndex;

        public FilterParser(string input)
        {
            var lexer = new FilterLexer(input);
            tokens = lexer.Tokenize();
        }

        public IFilterNode Parse()
        {
            if (tokens.Count > 0)
            {
                return Expression();
            }

            return null;
        }

        private IFilterNode Expression()
        {
            return OrExpression();
        }

        private IFilterNode OrExpression()
        {
            IFilterNode firstArgument = AndExpression();

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
            IFilterNode firstArgument = ComparisonExpression();

            if (Is(FilterTokenType.And))
            {
                return ParseAndExpression(firstArgument);
            }

            return firstArgument;
        }

        private IFilterNode ComparisonExpression()
        {
            IFilterNode firstArgument = PrimaryExpression();

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
            IFilterNode secondArgument = OrExpression();
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
                FilterToken comparison = Expect(FilterTokenType.ComparisonOperator);

                IFilterNode secondArgument = PrimaryExpression();

                return new ComparisonNode
                {
                    First = firstArgument,
                    FilterOperator = comparison.ToFilterOperator(),
                    Second = secondArgument
                };
            }

            FilterToken function = Expect(FilterTokenType.Function);

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
            IFilterNode secondArgument = ComparisonExpression();
            return new AndNode
            {
                First = firstArgument,
                Second = secondArgument
            };
        }

        private IFilterNode ParseStringExpression()
        {
            FilterToken stringToken = Expect(FilterTokenType.String);

            return new StringNode
            {
                Value = stringToken.Value
            };
        }
        private IFilterNode ParseStringExpressionUseIgnoreCase()
        {
            FilterToken stringToken = Expect(FilterTokenType.StringUseIgnoreCase);

            return new StringNode
            {
                Value = stringToken.Value,
                IgnoreCaseSensitive = true
            };
        }

        private IFilterNode ParseArrayExpression()
        {
            FilterToken stringToken = Expect(FilterTokenType.LeftSquareBracket);

            List<object> list = new List<object>();

            list.Add(Expression());

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
            FilterToken stringToken = Expect(FilterTokenType.Boolean);

            return new BooleanNode
            {
                Value = Convert.ToBoolean(stringToken.Value)
            };
        }

        private IFilterNode ParseNull()
        {
            FilterToken stringToken = Expect(FilterTokenType.Null);

            return new BooleanNode
            {
                Value = null
            };
        }

        private IFilterNode ParseNumberExpression()
        {
            FilterToken number = Expect(FilterTokenType.Number);

            return new NumberNode
            {
                Value = Convert.ToDouble(number.Value, CultureInfo.InvariantCulture)
            };
        }

        private IFilterNode ParsePropertyExpression()
        {
            FilterToken property = Expect(FilterTokenType.Property);

            return new PropertyNode
            {
                Name = property.Value
            };
        }

        private IFilterNode ParseDateTimeExpression()
        {
            FilterToken dateTime = Expect(FilterTokenType.DateTime);
            var acceptDates = new string[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };
            return new DateTimeNode
            {
                Value = DateTime.ParseExact(dateTime.Value, acceptDates, null, DateTimeStyles.None)
            };
        }
        private IFilterNode ParseTimeExpression()
        {
            FilterToken dateTime = Expect(FilterTokenType.Time);
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
            FilterToken function = Expect(FilterTokenType.Function);

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

            FilterToken token = Peek();
            currentTokenIndex++;
            return token;
        }

        private bool Is(FilterTokenType tokenType)
        {
            FilterToken token = Peek();
            return token != null && token.TokenType == tokenType;
        }

        private FilterToken Peek()
        {
            if (currentTokenIndex < tokens.Count)
            {
                return tokens[currentTokenIndex];
            }

            return null;
        }
    }
}