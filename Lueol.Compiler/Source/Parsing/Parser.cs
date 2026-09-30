using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;

class Parser
{
    public List<UseStmt> UseStmts { get; } = [];
    List<Token> _tokens;
    int _i = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Expr> Parse()
    {
        List<Expr> exprs = new List<Expr>();

        while (Check(TokenType.Use))
        {
            Position position = Current().Position;

            Next();

            string path = Current().Lexeme;

            Eat("Expected a string path", TokenType.String);

            Expect(TokenType.Semicolon);

            UseStmts.Add(new UseStmt(path, position));
        }

        while (Check(TokenType.Import))
        {
            Position position = Current().Position;

            Next();

            string path = Current().Lexeme;

            Eat("Expected a string path", TokenType.String);

            Expect(TokenType.Semicolon);

            exprs.Add(new ImportExpr(path, position));
        }

        while (NotAtEnd())
        {
            if (Check(TokenType.Use))
                throw new Error("use must appear before everying", Current().Position);

            if (Check(TokenType.Import))
                throw new Error("import must appear before everying besides use", Current().Position);

            exprs.Add(ParseExpr());
            Expect(TokenType.Semicolon);
        }

        return exprs;
    }

    Error ThrowUnexpected()
    {
        Token token = Current();
        string? keyword = Lexer.GetKeywordFromType(token.TokenType);
        string? symbol = Lexer.GetSymbolFromType(token.TokenType);
        if (keyword != null)
            throw new Error($"Unexpected keyword '{keyword}'", token.Position);
        else if (symbol != null)
            throw new Error($"Unexpected symbol '{symbol}'", token.Position);
        else
            throw new Error($"Unexpected token '{token.TokenType}'", token.Position);
    }

    string ParseName()
    {
        string name = Current().Lexeme;
        Eat("Expected name", TokenType.Identifier);
        return name;
    }

    List<Expr> ParseBody()
    {
        List<Expr> exprs = new List<Expr>();

        if (!Check(TokenType.LeftBrace))
        {
            exprs.Add(ParseExpr());
            return exprs;
        }

        Expect(TokenType.LeftBrace);

        while (NotAtEnd() && !Check(TokenType.RightBrace))
        {
            exprs.Add(ParseExpr());
            Expect(TokenType.Semicolon);
        }

        Expect(TokenType.RightBrace);

        return exprs;
    }

    List<string> ParseNames(TokenType end)
    {
        if (Match(end))
            return new List<string>();

        List<string> names = new List<string>()
            {
                ParseName()
            };

        while (Match(TokenType.Comma))
            names.Add(ParseName());

        Expect(end);

        return names;
    }

    List<string> ParseNames(TokenType start, TokenType end)
    {
        Expect(start);
        return ParseNames(end);
    }

    List<Expr> ParseArgs(TokenType start, TokenType end)
    {
        Expect(start);

        if (Match(end))
            return new List<Expr>();

        List<Expr> args = new List<Expr>()
            {
                ParseExpr()
            };

        while (Match(TokenType.Comma))
            args.Add(ParseExpr());

        Expect(end);

        return args;
    }

    bool Check(params TokenType[] types)
    {
        for (int i = 0; i < types.Length; i++)
            if (Current().TokenType == types[i])
                return true;
        return false;
    }

    Token Current() => _tokens[_i];
    Token Peek() => _tokens[_i + 1];
    bool NotAtEnd() => !Check(TokenType.Eof);
    bool PeekNotAtEnd() => _i + 1 < _tokens.Count;
    bool AtEnd() => Check(TokenType.Eof);
    void Next() => _i++;

    void Eat(string message, params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return;
        }
        throw new Error(message, Current().Position);
    }

    bool Match(params TokenType[] types)
    {
        if (Check(types))
        {
            Next();
            return true;
        }
        return false;
    }

    void Expect(TokenType type)
    {
        if (Check(type))
        {
            Next();
            return;
        }

        throw new Error($"Expected '{type}', got '{Current().TokenType}'", Current().Position);
    }

    // void Expect(TokenType type)
    // {
    //     if (Check(type))
    //     {
    //         Next();
    //         return;
    //     }
    //     string? keyword = Lexer.GetKeywordFromType(type);
    //     string? symbol = Lexer.GetSymbolFromType(type);
    //     if (keyword != null)
    //         throw new Error($"Expected keyword '{keyword}'", Current().Position);
    //     else if (symbol != null)
    //         throw new Error($"Expected symbol '{symbol}'", Current().Position);
    //     else
    //         throw new Error($"Expected token '{type}'", Current().Position);
    // }

    Expr ParsePrimary()
    {
        Token token = Current();

        if (Match(TokenType.Number))
        {
            if (long.TryParse(token.Lexeme, CultureInfo.InvariantCulture, out long longResult))
                return new IntExpr(longResult, token.Position);

            else if (double.TryParse(token.Lexeme, CultureInfo.InvariantCulture, out double doubleResult))
                return new FloatExpr(doubleResult, token.Position);

            throw new UnreachableException();
        }
        else if (Match(TokenType.String))
            return new StringExpr(token.Lexeme, token.Position);
        else if (Match(TokenType.True))
            return new BoolExpr(true, token.Position);
        else if (Match(TokenType.False))
            return new BoolExpr(false, token.Position);
        else if (Match(TokenType.Identifier))
            return new NameExpr(token.Lexeme, token.Position);
        else if (Match(TokenType.Nil))
            return new NilExpr(token.Position);
        else if (Match(TokenType.LeftParen))
        {
            Expr expr = ParseExpr();
            Expect(TokenType.RightParen);
            return expr;
        }
        else if (Match(TokenType.Let))
        {
            string name = ParseName();

            Expect(TokenType.Equal);

            Expr expr = ParseExpr();

            return new LetExpr(name, expr, token.Position);
        }

        else if (Match(TokenType.Const))
        {
            string name = ParseName();

            Expect(TokenType.Equal);

            Expr expr = ParseExpr();

            return new ConstExpr(name, expr, token.Position);
        }
        else if (Match(TokenType.If))
        {
            Expr condition = ParseExpr();

            List<Expr> body = ParseBody();

            List<Expr>? elseBody = null;

            if (Match(TokenType.Else))
                elseBody = ParseBody();

            return new IfExpr(condition, body, elseBody, token.Position);
        }

        else if (Match(TokenType.While))
        {
            Expr condition = ParseExpr();

            List<Expr> body = ParseBody();

            return new WhileExpr(condition, body, token.Position);
        }

        else if (Match(TokenType.Break))
        {
            if (Check(TokenType.Semicolon))
                return new BreakExpr(null, token.Position);

            Expr expr = ParseExpr();

            return new BreakExpr(expr, token.Position);
        }

        else if (Match(TokenType.Continue))
        {
            return new ContinueExpr(token.Position);
        }

        else if (Match(TokenType.Match))
        {
            MatchPattern ParsePattern()
            {
                if (Match(TokenType.Default))
                {
                    Expect(TokenType.Arrow);

                    Expr defaultResult = ParseExpr();

                    return new MatchDefault(defaultResult);
                }

                Expr pattern = ParseExpr();

                Expect(TokenType.Arrow);

                Expr result = ParseExpr();

                return new MatchArm(pattern, result);
            }

            Expr scrutinee = ParseExpr();

            Expect(TokenType.LeftBrace);

            if (Match(TokenType.RightBrace))
                return new MatchExpr(scrutinee, [], token.Position);

            List<MatchPattern> matchPatterns = [ParsePattern()];

            while (Match(TokenType.Comma))
                matchPatterns.Add(ParsePattern());

            Expect(TokenType.RightBrace);

            return new MatchExpr(scrutinee, matchPatterns, token.Position);
        }

        else if (Check(TokenType.LeftBrace))
        {
            List<Expr> body = ParseBody();
            return new BlockExpr(body, token.Position);
        }

        else if (Match(TokenType.Def))
        {
            List<string> parameters = [];

            if (Check(TokenType.LeftParen))
                parameters = ParseNames(TokenType.LeftParen, TokenType.RightParen);

            List<Expr> body = ParseBody();

            return new FunctionExpr(parameters, body, token.Position);
        }

        else if (Match(TokenType.Return))
        {
            if (Check(TokenType.Semicolon))
                return new ReturnExpr(null, token.Position);

            Expr expr = ParseExpr();

            return new ReturnExpr(expr, token.Position);
        }

        throw ThrowUnexpected();
    }

    Expr ParsePostfix()
    {
        Expr left = ParsePrimary();
        while (Check(TokenType.LeftParen, TokenType.LeftBracket, TokenType.Period))
        {
            if (Check(TokenType.LeftParen))
            {
                Position position = Current().Position;

                List<Expr> args = ParseArgs(TokenType.LeftParen, TokenType.RightParen);

                left = new CallExpr(left, args, position);

                continue;
            }

            if (Check(TokenType.LeftBracket))
            {
                Position position = Current().Position;

                Expect(TokenType.LeftBracket);

                Expr index = ParseExpr();

                Expect(TokenType.RightBracket);

                left = new IndexExpr(left, index, position);

                continue;
            }

            if (Check(TokenType.Period))
            {
                Position position = Current().Position;

                Next();

                string name = ParseName();

                left = new MemberExpr(left, name, position);

                continue;
            }

            break;
        }
        return left;
    }

    Expr ParseUnary()
    {
        if (Check(TokenType.Sub, TokenType.Bang, TokenType.BitwiseNot, TokenType.At))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            return new UnaryExpr(right, op.TokenType, op.Position);
        }
        return ParsePostfix();
    }

    Expr ParseTerm()
    {
        Expr left = ParseUnary();

        while (Check(TokenType.Mul, TokenType.Div, TokenType.Mod))
        {
            Token op = Current();

            Next();

            Expr right = ParseUnary();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseFactor()
    {
        Expr left = ParseTerm();

        while (Check(TokenType.Add, TokenType.Sub))
        {
            Token op = Current();

            Next();

            Expr right = ParseTerm();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseShift()
    {
        Expr left = ParseFactor();

        while (Check(TokenType.BitwiseLeftShift, TokenType.BitwiseRightShift))
        {
            Token op = Current();

            Next();

            Expr right = ParseFactor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseAnd()
    {
        Expr left = ParseBitwiseShift();

        while (Check(TokenType.BitwiseAnd))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseShift();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseXor()
    {
        Expr left = ParseBitwiseAnd();

        while (Check(TokenType.BitwiseXor))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseBitwiseOr()
    {
        Expr left = ParseBitwiseXor();

        while (Check(TokenType.BitwiseOr))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseXor();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseComparison()
    {
        Expr left = ParseBitwiseOr();

        while (Check(TokenType.Less, TokenType.Greater, TokenType.LessEq, TokenType.GreaterEq))
        {
            Token op = Current();

            Next();

            Expr right = ParseBitwiseOr();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseEquality()
    {
        Expr left = ParseComparison();

        while (Check(TokenType.NotEqual, TokenType.IsEqual))
        {
            Token op = Current();

            Next();

            Expr right = ParseComparison();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseAnd()
    {
        Expr left = ParseEquality();

        while (Check(TokenType.And))
        {
            Token op = Current();

            Next();

            Expr right = ParseEquality();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseOr()
    {
        Expr left = ParseAnd();

        while (Check(TokenType.Or))
        {
            Token op = Current();

            Next();

            Expr right = ParseAnd();

            left = new BinaryExpr(left, right, op.TokenType, op.Position);
        }

        return left;
    }

    Expr ParseFlow()
    {
        Expr left = ParseOr();

        while (Check(TokenType.Flow))
        {
            Position position = Current().Position;

            Next();

            Expr target = ParseOr();

            left = new FlowExpr(left, target, position);
        }

        return left;
    }

    Expr ParseAssign()
    {
        Expr left = ParseFlow();

        if (Match(TokenType.Equal))
        {
            Expr expr = ParseAssign();

            if (left is NameExpr nameExpr)
                return new AssignExpr(nameExpr.Name, expr, left.Position);

            else if (left is IndexExpr indexExpr)
                return new IndexSetExpr(indexExpr, expr);

            else if (left is MemberExpr memberExpr)
                return new MemberSetExpr(memberExpr, expr);

            throw new Error("Invalid assignment target", left.Position);
        }

        return left;
    }

    Expr ParseExpr() => ParseAssign();
}