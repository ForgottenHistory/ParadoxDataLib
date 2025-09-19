namespace ParadoxDataLib.Core.Tokenizer
{
    public struct Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Position { get; set; }

        public Token(TokenType type, string value, int line, int column, int position)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
            Position = position;
        }

        public bool IsOperator => Type == TokenType.Equals ||
                                  Type == TokenType.GreaterThan ||
                                  Type == TokenType.LessThan ||
                                  Type == TokenType.GreaterThanOrEqual ||
                                  Type == TokenType.LessThanOrEqual ||
                                  Type == TokenType.NotEqual;

        public bool IsValue => Type == TokenType.String ||
                               Type == TokenType.Number ||
                               Type == TokenType.Date ||
                               Type == TokenType.Identifier ||
                               Type == TokenType.Yes ||
                               Type == TokenType.No;

        public override string ToString()
        {
            return $"{Type}: {Value} at [{Line}:{Column}]";
        }
    }
}