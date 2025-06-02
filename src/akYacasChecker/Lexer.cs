using System.Globalization;
using static akUtil.Util;

namespace akExtractMatMultSolution
{
    //  operators are ordered in decreasing priority
    public enum TokenType 
    { 
        unknown, 
        variable, 
        number, 
        imaginary, 
        leftBracket, 
        rightBracket, 
        times, 
        add, 
        subtract, 
        end, 
        show, 
        error 
    };

    public struct Token
    {
        public TokenType type;
        public string text;
        public int lineCount;
        public int charColumn;

        public readonly double Double()
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                throw new SyntaxErrorException($"Cannot process '{text}' as double", this);
            }

            return result;
        }
    }

    /// <summary>
    /// Class to tokenize the string to be parsed by the Parser
    /// </summary>
    class Lexer
    {
        private readonly char[] scanBuffer;
        private int pos;
        private readonly int posMax;
        private char cc;          //  current character
        private Token currentToken;

        private const string spaceChars = " \"\t\r\n\f";
        private const string identifierChars = "abcpP0123456789";
        private const string commentChars = "%;#";
        private const char endMarker = ';';

        public readonly bool showLineNumber = false;

        public Lexer(string _input)
        {
            scanBuffer = (_input + endMarker).ToCharArray();
            posMax = scanBuffer.Length - 1;
            pos = -1;
            cc = '\0';
            currentToken = new Token
            {
                lineCount = 1,
                charColumn = -1,
                type = TokenType.unknown
            };

            if (!_input.Contains($"{endMarker}"))
            {
                throw new SyntaxErrorException($"No end marker '{endMarker}' in  line", currentToken);
            }
        }

        public Token GetNextToken()
        {
            string text;

            Assert(currentToken.type != TokenType.end);
            Assert(pos <  posMax);

            RubberBlanks();

            if (cc == '(')
            {
                currentToken.type = TokenType.leftBracket;
                currentToken.text = "(";
            }
            else if (cc == ')')
            {
                currentToken.type = TokenType.rightBracket;
                currentToken.text = ")";
            }
            else if (cc == '*')
            {
                currentToken.type = TokenType.times;
                currentToken.text = "*";
            }
            else if (cc == '+')
            {
                currentToken.type = TokenType.add;
                currentToken.text = "+";
            }
            else if (cc == '-')
            {
                currentToken.type = TokenType.subtract;
                currentToken.text = "-";
            }
            else if (IsDigitChar())
            {
                text = "";
                while (IsDigitChar()) 
                {
                    text += cc;
                    NextChar();
                }

                if (cc == '.')
                {
                    text += cc;
                    NextChar();
                    while (IsDigitChar()) 
                    {
                        text += cc;
                        NextChar();
                    }
                }

                currentToken.type = TokenType.number;
                if ((cc == 'i') || (cc == 'j'))
                {
                    currentToken.type = TokenType.imaginary;
                    NextChar();
                }

                cc = scanBuffer[--pos];

                currentToken.text = text;
            }
            else if (IsLiteralChar())
            {
                text = cc.ToString();
                do
                {
                    NextChar();
                    text += cc;
                }
                while (IsLiteralChar());

                cc = scanBuffer[--pos];

                currentToken.type = TokenType.variable;
                currentToken.text = text.Substring(0, text.Length - 1);

            }
            else if (cc == endMarker)
            {
                currentToken.type = TokenType.end;
                currentToken.text = "End";
            }
            else
            {
                currentToken.type = TokenType.unknown;
                currentToken.text = cc.ToString();
            }

            return currentToken;
        }

        private bool IsLiteralChar()
        {
            return identifierChars.IndexOf(cc) >= 0;
        }
        private bool IsDigitChar()
        {
            return "0123456789".IndexOf(cc) >= 0;
        }

        private void NextChar()
        {
            if (pos < posMax)
            {
                cc = scanBuffer[++pos];
                currentToken.charColumn++;
            }
        }

        /// <summary>
        /// rubber blanks and spaces and comments
        /// </summary>
        private void RubberBlanks()
        {
            bool inComment = false;

            //  the very last input character is endMarker and not a space
            do
            {
                NextChar();

                if (cc == '\n')
                {
                    inComment = false;
                    currentToken.lineCount++;
                    currentToken.charColumn = 0;
                }
                else
                {
                    inComment |= (commentChars.IndexOf(cc) >= 0);
                }
            }
            while ((pos < posMax) && (inComment || (spaceChars.IndexOf(cc) >= 0)));
        }

        public string ShowToken(TokenType type = TokenType.show)
        {
            TokenType tt = type != TokenType.show ? type : currentToken.type;

            string s = tt switch
            {
                TokenType.add => "+",
                TokenType.end => "end",
                TokenType.error => "<error>",
                TokenType.imaginary => $"{currentToken.text}j",
                TokenType.leftBracket => "(",
                TokenType.number => currentToken.text,
                TokenType.rightBracket => ")",
                TokenType.subtract => "-",
                TokenType.times => "*",
                TokenType.show => "show",
                TokenType.unknown => "unknown",
                TokenType.variable => currentToken.text,
                _ => "???",
            };

            if (showLineNumber)
            {
                s = currentToken.lineCount + ":" + currentToken.charColumn + " " + s;
            }
            else
            {
                s = currentToken.charColumn + " " + s;
            }
            return s;
        }
    }

    //  end of class Lexer.cs
}
