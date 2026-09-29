internal enum TokenType
{
    String, Number, Identifier,

    True, False, Nil, Let, Const, If, Else,
    While, Break, Continue, Match, Default,

    Add, Sub, Mul, Div, Mod,
    IsEqual, NotEqual, Less, Greater,
    LessEq, GreaterEq, And, Or, Bang,
    BitwiseNot, BitwiseLeftShift, BitwiseRightShift,
    BitwiseAnd, BitwiseOr, BitwiseXor,

    Equal, Semicolon, LeftBracket, RightBracket,
    LeftBrace, RightBrace, LeftParen, RightParen,
    Comma, Period, Hash, Arrow, At, UnderScore,

    Eof,
}

internal record Token(TokenType TokenType, string Lexeme, Position Position);