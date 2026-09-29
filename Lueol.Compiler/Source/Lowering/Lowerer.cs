using System.Text.RegularExpressions;

class VariableSymbol
{
    public VariableSymbol(CsLocal csLocal, bool isConst)
    {
        CsLocal = csLocal;
        IsConst = isConst;
    }

    public CsLocal CsLocal { get; }
    public bool IsConst { get; }
}

class Scopes
{
    Stack<Dictionary<string, VariableSymbol>> _scopes = [];

    public Scopes() => _scopes.Push(new());
    public void BeginScope() => _scopes.Push(new());
    public void EndScope() => _scopes.Pop();

    public bool TryResolve(string name, out VariableSymbol? variableSymbol)
    {
        foreach (var scope in _scopes)
            if (scope.TryGetValue(name, out variableSymbol))
                return true;

        variableSymbol = null;
        return false;
    }

    public VariableSymbol Resolve(string name, Position position)
    {
        if (!TryResolve(name, out VariableSymbol? symbol))
            throw new Error($"'{name}' does not exist", position);

        return symbol!;
    }

    public void Define(CsLocal csLocal, bool isConst, Position position)
    {
        var scope = _scopes.Peek();
        if (scope.ContainsKey(csLocal.Name))
            throw new Error($"'{csLocal.Name}' is already defined", position);
        scope.Add(csLocal.Name, new VariableSymbol(csLocal, isConst));
    }

    public CsLocal Set(string name, Position position)
    {
        var symbol = Resolve(name, position);
        if (symbol.IsConst)
            throw new Error($"'{name}' cannot be reassigned", position);
        return symbol.CsLocal;
    }
}

class Lowerer
{
    public List<CsInstruction> CsInstructions { get; } = [];
    public Dictionary<CsLocal, string> MangledNames = new(ReferenceEqualityComparer.Instance);
    Scopes _scopes = new Scopes();
    long _nextLocalIndex = 0;
    Stack<CsLocal> _loopStack = [];

    public CsValue LowerExprs(List<Expr> exprs)
    {
        for (int i = 0; i < exprs.Count; i++)
        {
            Expr expr = exprs[i];
            if (i == exprs.Count - 1)
                return LowerExpr(expr);
            LowerExpr(expr);
        }
        return new CsNil();
    }

    public void MangleNames()
    {
        foreach (var instruction in CsInstructions)
        {
            if (instruction is CsDeclare declare)
            {
                CsLocal csLocal = declare.CsLocal;

                MangledNames.Add(csLocal, $"{csLocal.Name}_{_nextLocalIndex++}");
            }
        }
    }

    CsValue LowerExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return new CsLiteral(intExpr.Value);

            case FloatExpr floatExpr:
                return new CsLiteral(floatExpr.Value);

            case StringExpr stringExpr:
                return new CsLiteral(stringExpr.Value);

            case BoolExpr boolExpr:
                return new CsLiteral(boolExpr.Value);

            case NilExpr:
                return new CsNil();

            case NameExpr nameExpr:
                return _scopes.Resolve(nameExpr.Name, nameExpr.Position).CsLocal;

            case UnaryExpr unaryExpr:
                {
                    CsValue right = LowerExpr(unaryExpr.Right);
                    return new CsUnary(unaryExpr.Op, right, unaryExpr.Position);
                }

            case BinaryExpr binaryExpr:
                {
                    if (binaryExpr.Op == TokenType.And)
                        return LowerAnd(binaryExpr);
                    else if (binaryExpr.Op == TokenType.Or)
                        return LowerOr(binaryExpr);
                    CsValue left = LowerExpr(binaryExpr.Left);
                    CsValue right = LowerExpr(binaryExpr.Right);
                    return new CsBinary(binaryExpr.Op, left, right, binaryExpr.Position);
                }

            case LetExpr letExpr:
                {
                    CsValue value = LowerExpr(letExpr.Expr);

                    CsLocal csLocal = new CsLocal(letExpr.Name);

                    _scopes.Define(csLocal, false, letExpr.Position);

                    CsInstructions.Add(new CsDeclare(csLocal, false, value));

                    return csLocal;
                }

            case ConstExpr constExpr:
                {
                    CsValue value = LowerExpr(constExpr.Expr);

                    CsLocal csLocal = new CsLocal(constExpr.Name);

                    _scopes.Define(csLocal, true, constExpr.Position);

                    CsInstructions.Add(new CsDeclare(csLocal, true, value));

                    return csLocal;
                }

            case AssignExpr assignExpr:
                {
                    CsValue value = LowerExpr(assignExpr.Expr);

                    CsLocal csLocal = _scopes.Set(assignExpr.Name, assignExpr.Position);

                    CsInstructions.Add(new CsAssign(csLocal, value));

                    return csLocal;
                }

            case IfExpr ifExpr:
                {
                    CsLocal csLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(csLocal, false, new CsNil()));

                    // IF

                    CsValue condition = LowerExpr(ifExpr.Expr);

                    CsInstructions.Add(new CsIfStart(condition));

                    _scopes.BeginScope();

                    CsValue ifLast = LowerExprs(ifExpr.IfBody);

                    CsInstructions.Add(new CsAssign(csLocal, ifLast));

                    _scopes.EndScope();

                    CsInstructions.Add(new CsIfEnd());

                    // ELSE

                    if (ifExpr.ElseBody != null)
                    {
                        CsInstructions.Add(new CsElseStart());

                        _scopes.BeginScope();

                        CsValue elselast = LowerExprs(ifExpr.ElseBody);

                        CsInstructions.Add(new CsAssign(csLocal, elselast));

                        _scopes.EndScope();

                        CsInstructions.Add(new CsElseEnd());
                    }

                    return csLocal;
                }

            case WhileExpr whileExpr:
                {
                    CsLocal csLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(csLocal, false, new CsNil()));

                    CsInstructions.Add(new CsWhileStart());

                    CsValue condition = LowerExpr(whileExpr.Expr);

                    CsInstructions.Add(new CsWhileCondition(condition));

                    _scopes.BeginScope();

                    _loopStack.Push(csLocal);

                    CsValue whileLast = LowerExprs(whileExpr.Body);
                    CsInstructions.Add(new CsAssign(csLocal, whileLast));

                    _loopStack.Pop();

                    _scopes.EndScope();

                    CsInstructions.Add(new CsWhileEnd());

                    return csLocal;
                }

            case BreakExpr breakExpr:
                {
                    CsValue csValue = breakExpr.Expr != null
                        ? LowerExpr(breakExpr.Expr)
                        : new CsNil();

                    CsLocal loopLocal = _loopStack.Peek();

                    CsInstructions.Add(new CsAssign(loopLocal, csValue));
                    CsInstructions.Add(new CsBreak());

                    return csValue;
                }

            case ContinueExpr:
                CsInstructions.Add(new CsContinue());
                return new CsNil();

            case MatchExpr matchExpr:
                {
                    CsLocal resultLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(resultLocal, false, new CsNil()));

                    CsValue scrutinee = LowerExpr(matchExpr.Scutinee);

                    CsLocal scrutineeLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(scrutineeLocal, false, scrutinee));

                    LowerMatchPatterns(scrutineeLocal, resultLocal, matchExpr.Patterns, 0);

                    return resultLocal;
                }

            case BlockExpr blockExpr:
                {
                    CsLocal resultLocal = new CsLocal("result_local");

                    CsInstructions.Add(new CsDeclare(resultLocal, false, new CsNil()));

                    CsInstructions.Add(new CsBlockStart());

                    _scopes.BeginScope();

                    CsValue last = LowerExprs(blockExpr.Exprs);
                    CsInstructions.Add(new CsAssign(resultLocal, last));

                    _scopes.EndScope();

                    CsInstructions.Add(new CsBlockEnd());

                    return resultLocal;
                }
        }

        throw new Error($"'{expr.GetType().Name}' is an invalid expression", expr.Position);
    }

    CsValue LowerAnd(BinaryExpr binaryExpr)
    {
        CsLocal csLocal = new CsLocal("lowerer_temp");

        CsInstructions.Add(new CsDeclare(csLocal, false, new CsLiteral(false)));

        CsValue left = LowerExpr(binaryExpr.Left);

        CsInstructions.Add(new CsIfStart(left));

        _scopes.BeginScope();

        CsValue right = LowerExpr(binaryExpr.Right);

        CsInstructions.Add(new CsIfStart(right));
        CsInstructions.Add(new CsAssign(csLocal, new CsLiteral(true)));
        CsInstructions.Add(new CsIfEnd());

        _scopes.EndScope();

        CsInstructions.Add(new CsIfEnd());

        return csLocal;
    }

    CsValue LowerOr(BinaryExpr binaryExpr)
    {
        CsLocal csLocal = new CsLocal("lowerer_temp");

        CsInstructions.Add(new CsDeclare(csLocal, false, new CsLiteral(false)));

        CsValue left = LowerExpr(binaryExpr.Left);

        CsInstructions.Add(new CsIfStart(left));
        CsInstructions.Add(new CsAssign(csLocal, new CsLiteral(true)));
        CsInstructions.Add(new CsIfEnd());

        CsInstructions.Add(new CsElseStart());

        _scopes.BeginScope();

        CsValue right = LowerExpr(binaryExpr.Right);

        CsInstructions.Add(new CsIfStart(right));
        CsInstructions.Add(new CsAssign(csLocal, new CsLiteral(true)));
        CsInstructions.Add(new CsIfEnd());

        _scopes.EndScope();

        CsInstructions.Add(new CsElseEnd());

        return csLocal;
    }

    void LowerMatchPatterns(CsLocal scrutinee, CsLocal result, List<MatchPattern> matchPatterns, int index)
    {
        MatchPattern pattern = matchPatterns[index];

        if (pattern is MatchDefault matchDefault)
        {
            CsValue value = LowerExpr(matchDefault.Result);
            CsInstructions.Add(new CsAssign(result, value));
            return;
        }

        MatchArm arm = (MatchArm)pattern;

        CsValue armPattern = LowerExpr(arm.Pattern);

        CsValue condition = new CsBinary(
            TokenType.IsEqual,
            scrutinee,
            armPattern,
            arm.Pattern.Position
        );

        CsInstructions.Add(new CsIfStart(condition));

        _scopes.BeginScope();

        CsValue armResult = LowerExpr(arm.Result);
        CsInstructions.Add(new CsAssign(result, armResult));

        _scopes.EndScope();

        CsInstructions.Add(new CsIfEnd());

        if (index + 1 < matchPatterns.Count)
        {
            CsInstructions.Add(new CsElseStart());

            LowerMatchPatterns(scrutinee, result, matchPatterns, index + 1);

            CsInstructions.Add(new CsElseEnd());
        }
    }
}