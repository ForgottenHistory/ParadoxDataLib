using System;
using System.Collections.Generic;
using System.Text;

namespace ParadoxDataLib.Core.Tokenizer
{
    public class Lexer
    {
        private readonly string _input;
        private int _position;
        private int _line;
        private int _column;
        private readonly List<Token> _tokens;

        public Lexer(string input)
        {
            _input = input ?? string.Empty;
            _position = 0;
            _line = 1;
            _column = 1;
            _tokens = new List<Token>();
        }

        public List<Token> Tokenize()
        {
            _tokens.Clear();
            _position = 0;
            _line = 1;
            _column = 1;

            while (_position < _input.Length)
            {
                var token = ReadNextToken();
                if (token.Type != TokenType.None &&
                    token.Type != TokenType.Whitespace)
                {
                    _tokens.Add(token);
                }
            }

            _tokens.Add(new Token(TokenType.EndOfFile, "", _line, _column, _position));
            return _tokens;
        }

        private Token ReadNextToken()
        {
            SkipWhitespace();

            if (_position >= _input.Length)
                return new Token(TokenType.EndOfFile, "", _line, _column, _position);

            var currentChar = CurrentChar();
            var startPosition = _position;
            var startColumn = _column;
            var startLine = _line;

            // Comments
            if (currentChar == '#')
            {
                return ReadComment(startLine, startColumn, startPosition);
            }

            // Quoted strings
            if (currentChar == '"')
            {
                return ReadQuotedString(startLine, startColumn, startPosition);
            }

            // Operators
            if (currentChar == '=')
            {
                Advance();
                return new Token(TokenType.Equals, "=", startLine, startColumn, startPosition);
            }

            // RGB color syntax: { r g b } - check before regular braces
            if (currentChar == '{' && IsRgbColor())
            {
                return ReadRgbColor(startLine, startColumn, startPosition);
            }

            if (currentChar == '{')
            {
                Advance();
                return new Token(TokenType.LeftBrace, "{", startLine, startColumn, startPosition);
            }

            if (currentChar == '}')
            {
                Advance();
                return new Token(TokenType.RightBrace, "}", startLine, startColumn, startPosition);
            }

            if (currentChar == '>')
            {
                Advance();
                if (CurrentChar() == '=')
                {
                    Advance();
                    return new Token(TokenType.GreaterThanOrEqual, ">=", startLine, startColumn, startPosition);
                }
                return new Token(TokenType.GreaterThan, ">", startLine, startColumn, startPosition);
            }

            if (currentChar == '<')
            {
                Advance();
                if (CurrentChar() == '=')
                {
                    Advance();
                    return new Token(TokenType.LessThanOrEqual, "<=", startLine, startColumn, startPosition);
                }
                return new Token(TokenType.LessThan, "<", startLine, startColumn, startPosition);
            }

            if (currentChar == '!')
            {
                Advance();
                if (CurrentChar() == '=')
                {
                    Advance();
                    return new Token(TokenType.NotEqual, "!=", startLine, startColumn, startPosition);
                }
            }

            // Numbers (including negative)
            if (char.IsDigit(currentChar) || (currentChar == '-' && _position + 1 < _input.Length && char.IsDigit(_input[_position + 1])))
            {
                return ReadNumber(startLine, startColumn, startPosition);
            }

            // Identifiers and keywords
            if (char.IsLetter(currentChar) || currentChar == '_')
            {
                return ReadIdentifierOrKeyword(startLine, startColumn, startPosition);
            }

            // Unknown character, skip it
            Advance();
            return new Token(TokenType.None, "", startLine, startColumn, startPosition);
        }

        private Token ReadComment(int line, int column, int position)
        {
            var sb = new StringBuilder();
            Advance(); // Skip #

            while (!IsAtEnd() && CurrentChar() != '\n')
            {
                sb.Append(CurrentChar());
                Advance();
            }

            return new Token(TokenType.Comment, sb.ToString().Trim(), line, column, position);
        }

        private Token ReadQuotedString(int line, int column, int position)
        {
            var sb = new StringBuilder();
            Advance(); // Skip opening quote

            while (!IsAtEnd() && CurrentChar() != '"')
            {
                if (CurrentChar() == '\\' && Peek() == '"')
                {
                    Advance(); // Skip backslash
                    sb.Append('"');
                    Advance();
                }
                else
                {
                    sb.Append(CurrentChar());
                    Advance();
                }
            }

            if (!IsAtEnd())
                Advance(); // Skip closing quote

            return new Token(TokenType.String, sb.ToString(), line, column, position);
        }

        private Token ReadNumber(int line, int column, int position)
        {
            var sb = new StringBuilder();

            if (CurrentChar() == '-')
            {
                sb.Append(CurrentChar());
                Advance();
            }

            while (!IsAtEnd() && (char.IsDigit(CurrentChar()) || CurrentChar() == '.'))
            {
                sb.Append(CurrentChar());
                Advance();
            }

            var value = sb.ToString();

            // Check if it's a date (YYYY.MM.DD)
            if (IsDate(value))
            {
                return new Token(TokenType.Date, value, line, column, position);
            }

            return new Token(TokenType.Number, value, line, column, position);
        }

        private Token ReadIdentifierOrKeyword(int line, int column, int position)
        {
            var sb = new StringBuilder();

            while (!IsAtEnd() && (char.IsLetterOrDigit(CurrentChar()) || CurrentChar() == '_' || CurrentChar() == ':'))
            {
                sb.Append(CurrentChar());
                Advance();
            }

            var value = sb.ToString();

            // Check for special keywords
            if (value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return new Token(TokenType.Yes, value, line, column, position);
            }

            if (value.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return new Token(TokenType.No, value, line, column, position);
            }

            // Check if it might be part of a date or continued reading
            if (!IsAtEnd() && CurrentChar() == '.')
            {
                var checkDate = TryReadDate(value, line, column, position);
                if (checkDate.Type == TokenType.Date)
                    return checkDate;
            }

            return new Token(TokenType.Identifier, value, line, column, position);
        }

        private Token TryReadDate(string firstPart, int line, int column, int position)
        {
            var sb = new StringBuilder(firstPart);
            var savedPosition = _position;
            var savedColumn = _column;

            // Try to read a date format (YYYY.MM.DD)
            if (CurrentChar() == '.')
            {
                sb.Append(CurrentChar());
                Advance();

                // Read month
                while (!IsAtEnd() && char.IsDigit(CurrentChar()))
                {
                    sb.Append(CurrentChar());
                    Advance();
                }

                if (!IsAtEnd() && CurrentChar() == '.')
                {
                    sb.Append(CurrentChar());
                    Advance();

                    // Read day
                    while (!IsAtEnd() && char.IsDigit(CurrentChar()))
                    {
                        sb.Append(CurrentChar());
                        Advance();
                    }

                    var dateStr = sb.ToString();
                    if (IsDate(dateStr))
                    {
                        return new Token(TokenType.Date, dateStr, line, column, position);
                    }
                }
            }

            // Revert if not a valid date
            _position = savedPosition;
            _column = savedColumn;
            return new Token(TokenType.Identifier, firstPart, line, column, position);
        }

        private bool IsDate(string value)
        {
            var parts = value.Split('.');
            if (parts.Length != 3) return false;

            return int.TryParse(parts[0], out var year) &&
                   int.TryParse(parts[1], out var month) &&
                   int.TryParse(parts[2], out var day) &&
                   year > 0 && year < 10000 &&
                   month >= 1 && month <= 12 &&
                   day >= 1 && day <= 31;
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd() && char.IsWhiteSpace(CurrentChar()))
            {
                if (CurrentChar() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        private char CurrentChar()
        {
            return _position < _input.Length ? _input[_position] : '\0';
        }

        private char Peek()
        {
            return _position + 1 < _input.Length ? _input[_position + 1] : '\0';
        }

        private void Advance()
        {
            if (_position < _input.Length)
            {
                if (CurrentChar() == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        private bool IsAtEnd()
        {
            return _position >= _input.Length;
        }

        private bool IsRgbColor()
        {
            // Look ahead to see if this is { r g b } pattern with valid RGB values (0-255)
            var savedPosition = _position;
            var savedColumn = _column;
            var savedLine = _line;

            try
            {
                if (CurrentChar() != '{') return false;
                Advance(); // Skip {
                SkipWhitespace();

                // Expect exactly 3 numbers separated by whitespace, each 0-255
                var values = new List<int>();
                for (int i = 0; i < 3; i++)
                {
                    if (!char.IsDigit(CurrentChar())) return false;

                    // Read the number
                    var numberStart = _position;
                    while (!IsAtEnd() && char.IsDigit(CurrentChar()))
                        Advance();

                    var numberStr = _input.Substring(numberStart, _position - numberStart);
                    if (!int.TryParse(numberStr, out var value) || value < 0 || value > 255)
                        return false;

                    values.Add(value);

                    if (i < 2)
                    {
                        SkipWhitespace();
                        if (IsAtEnd()) return false;
                    }
                }

                SkipWhitespace();

                // Must have exactly 3 values and close with }
                if (values.Count != 3 || CurrentChar() != '}')
                    return false;

                // Additional check: look for more numbers after the closing brace
                // If there are more numbers, this is likely not an RGB color
                var tempPos = _position + 1;
                while (tempPos < _input.Length && char.IsWhiteSpace(_input[tempPos]))
                    tempPos++;

                if (tempPos < _input.Length && char.IsDigit(_input[tempPos]))
                    return false; // More numbers follow, not RGB

                return true;
            }
            finally
            {
                // Restore position regardless of outcome
                _position = savedPosition;
                _column = savedColumn;
                _line = savedLine;
            }
        }

        private Token ReadRgbColor(int line, int column, int position)
        {
            var sb = new StringBuilder();

            // Read entire { r g b } structure
            sb.Append(CurrentChar()); // {
            Advance();

            while (!IsAtEnd() && CurrentChar() != '}')
            {
                sb.Append(CurrentChar());
                Advance();
            }

            if (!IsAtEnd())
            {
                sb.Append(CurrentChar()); // }
                Advance();
            }

            return new Token(TokenType.Color, sb.ToString().Trim(), line, column, position);
        }
    }
}