

using System;
using System.Collections.Generic;
using System.IO;

namespace akExtractMatMultSolution
{
    /// <summary>
    /// Class to convert an expression into a tree of AstAstNode elements
    /// </summary>
    class Parser : Util
    {
        private Lexer lex;   //  scanner/lexer for expression tokenizing
        private Token token;

        private readonly int debugLevel = 0;

        private Stack<Token> tokenStack;

        private void NextToken()
        {
            if (tokenStack.Count > 0)
            {
                token = tokenStack.Pop();
            }
            else
            {
                token = lex.GetNextToken();
            }

            if (debugLevel > 4)
            {
                o(lex.ShowToken());
            }
        }

        public AstNode Parse(string s, int lineCount)
        {
            AstNode ret = null;

            try
            {
                lex = new Lexer(s);
                tokenStack = new Stack<Token>();

                NextToken();

                if (token.type == TokenType.end)
                {
                    SyntaxError("Expression is empty");
                }
                else
                {
                    ret = ParseExpression();
                }

                if (tokenStack.Count > 0) 
                {
                    throw new ArgumentException("TokenStack is not empty!");
                }
            }
            catch (SyntaxErrorException e)
            {
                o();
                if (e.charColumn > 0)
                {
                    o($"Syntax error in line {lineCount} at char column {e.charColumn}:");
                    o($"{s}");
                    o($"{new string(' ', e.charColumn)}^");
                }
                else
                {
                    o($"Syntax error in line {lineCount}:");
                    o($"{s}");
                }
                o($"{e.Message}");
                o();
            }

            return ret;
        }

        private AstNode ParseExpression()
        {
            AstNode ret = ParseTerm();
            bool done = false;

            while (!done)
            {
                if (token.type == TokenType.add)
                {
                    NextToken();
                    AstNode right = ParseTerm();

                    if ((ret is ComplexNode cLeft) && (right is ComplexNode cRight))
                    {
                        ret = new ComplexNode(cLeft.Value + cRight.Value);
                    }
                    else
                    {
                        ret = new BinaryOperationNode(ret, TokenType.add, right);
                    }
                }
                else if (token.type == TokenType.subtract)
                {
                    NextToken();
                    AstNode right = ParseTerm();

                    if ((ret is ComplexNode cLeft) && (right is ComplexNode cRight))
                    {
                        ret = new ComplexNode(cLeft.Value - cRight.Value);
                    }
                    else
                    {
                        ret = new BinaryOperationNode(ret, TokenType.subtract, right);
                    }
                }
                else
                {
                    done = true;
                }
            }

            return ret;
        }

        private AstNode ParseTerm()
        {
            AstNode ret = ParseFactor();

            while (token.type == TokenType.times)
            {
                NextToken();
                AstNode right = ParseFactor();

                if ((ret is ComplexNode cLeft) && (right is ComplexNode cRight))
                {
                    ret = new ComplexNode(cLeft.Value * cRight.Value);
                }
                else
                {
                    ret = new BinaryOperationNode(ret, TokenType.times, right);
                }
            }

            return ret;
        }

        private AstNode ParseFactor()
        {
            AstNode ret = null;

            if (token.type == TokenType.add)
            {
                //  unary '+' ignored
                NextToken();
                ret = ParseFactor();
            }
            else if (token.type == TokenType.subtract)
            {
                NextToken();
                ret = ParseFactor();

                if (ret is ComplexNode cplxNd)
                {
                    ret = new ComplexNode(-cplxNd.Value);
                }
                else
                {
                    ret = new UnaryOperationNode(TokenType.subtract, ret);
                }
            }
            else if (token.type == TokenType.leftBracket)
            {
                NextToken();
                ret = ParseExpression();

                if (token.type != TokenType.rightBracket)
                {
                    SyntaxError("Missing closing bracket ')'");
                }
                NextToken();
            }
            else if (token.type == TokenType.variable)
            {
                ret = new VariableNode(token.text);
                NextToken();
            }
            else if (token.type == TokenType.number)
            {
                double real = token.Double();
                NextToken();

                ret = new ComplexNode((Coefficient)real);
            }
            else if (token.type == TokenType.imaginary)
            {
                double imag = token.Double();
                NextToken();

                ret = new ComplexNode(new Coefficient(real: 0, imaginary: imag));
            }

            return ret;
        }

        private void SyntaxError(string msg)
        {
            throw new SyntaxErrorException(msg, token);
        }
    }

    class SyntaxErrorException : Exception
    {
        public readonly int lineCount;
        public readonly int charColumn;

        public SyntaxErrorException(Token token)
        {
            lineCount = token.lineCount;
            charColumn = token.charColumn;
        }

        public SyntaxErrorException(string message, Token token)
            : base(message)
        {
            lineCount = token.lineCount;
            charColumn = token.charColumn;
        }

        public SyntaxErrorException(string message, Exception inner, Token token)
            : base(message, inner)
        {
            lineCount = token.lineCount;
            charColumn = token.charColumn;
        }
    }

    //  end of class Parser.cs
}
