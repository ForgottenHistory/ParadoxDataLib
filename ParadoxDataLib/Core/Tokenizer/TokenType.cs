namespace ParadoxDataLib.Core.Tokenizer
{
    public enum TokenType
    {
        None,
        Identifier,
        String,
        Number,
        Date,
        Equals,
        LeftBrace,
        RightBrace,
        Comment,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        NotEqual,
        Color,
        EndOfFile,
        NewLine,
        Whitespace,
        Yes,
        No
    }
}