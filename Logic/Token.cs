namespace LogicExporter.Logic;

public sealed record Token(
    TokenType Type,
    string Text,
    int Position);

public enum TokenType
{
    Not,
    And,
    Or,
    Conditional,
    Biconditional,
    Box,
    Diamond,
    ForAll,
    Exists,
    Equal,
    NotEqual,
    LeftParen,
    RightParen,
    Comma,
    SentenceLetter,
    TermSymbol,
    EndOfInput
}
