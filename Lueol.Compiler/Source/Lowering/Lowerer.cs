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
    public List<UseStmt> UseStmts { get; } = [];
    public Dictionary<CsLocal, string> MangledNames = new(ReferenceEqualityComparer.Instance);
    Scopes _scopes = new Scopes();
    long _nextLocalIndex = 0;
    Stack<CsLocal> _loopStack = [];
    Stack<CsLocal> _functionStack = [];
    long _nextFunctionIndex = 0;
    Dictionary<string, bool> _compiledFiles = [];

    public CsValue LowerExprs(List<Expr> exprs)
    {
        for (int i = 0; i < exprs.Count; i++)
        {
            Expr expr = exprs[i];
            var lowered = LowerExpr(expr);
            if (i == exprs.Count - 1)
                return lowered.Value;

            if (!lowered.IsMaterialized)
            {
                CsLocal temp = new CsLocal("lowerer_temp");
                CsInstructions.Add(new CsDeclare(temp, false, lowered.Value));
            }
        }
        return new CsNil();
    }

    public CsValue LowerFile(string path, Position? position)
    {
        path = Path.GetFullPath(path);

        if (!File.Exists(path))
            throw new Error($"File '{path}' does not exist", position);

        if (_compiledFiles.TryGetValue(path, out bool finished))
        {
            if (!finished)
                throw new Error($"Circular import detected: '{path}'", position);
            return new CsNil();
        }

        _compiledFiles[path] = false;

        List<Token> tokens = new Lexer(File.ReadAllText(path), path).Lex();
        Parser parser = new Parser(tokens);
        List<Expr> exprs = parser.Parse();

        UseStmts.AddRange(parser.UseStmts);

        Sematics.Check(exprs);

        CsValue last = LowerExprs(exprs);

        _compiledFiles[path] = true;

        return last;
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

    (CsValue Value, bool IsMaterialized) LowerExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return (new CsLiteral(intExpr.Value), true);

            case FloatExpr floatExpr:
                return (new CsLiteral(floatExpr.Value), true);

            case StringExpr stringExpr:
                return (new CsLiteral(stringExpr.Value), true);

            case BoolExpr boolExpr:
                return (new CsLiteral(boolExpr.Value), true);

            case NilExpr:
                return (new CsNil(), true);

            case NameExpr nameExpr:
                {
                    if (_scopes.TryResolve(nameExpr.Name, out var symbol))
                        return (symbol!.CsLocal, true);
                    return (new CsGetGlobal(nameExpr.Name, nameExpr.Position), false);
                }

            case UnaryExpr unaryExpr:
                {
                    CsValue right = LowerExpr(unaryExpr.Right).Value;
                    return (new CsUnary(unaryExpr.Op, right, unaryExpr.Position), false);
                }

            case BinaryExpr binaryExpr:
                {
                    if (binaryExpr.Op == TokenType.And)
                        return (LowerAnd(binaryExpr), true);
                    else if (binaryExpr.Op == TokenType.Or)
                        return (LowerOr(binaryExpr), true);

                    CsValue left = LowerExpr(binaryExpr.Left).Value;
                    CsValue right = LowerExpr(binaryExpr.Right).Value;

                    return (new CsBinary(binaryExpr.Op, left, right, binaryExpr.Position), false);
                }

            case LetExpr letExpr:
                {
                    CsValue value = LowerExpr(letExpr.Expr).Value;

                    CsLocal csLocal = new CsLocal(letExpr.Name);

                    _scopes.Define(csLocal, false, letExpr.Position);

                    CsInstructions.Add(new CsDeclare(csLocal, false, value));

                    return (csLocal, true);
                }

            case ConstExpr constExpr:
                {
                    CsValue value = LowerExpr(constExpr.Expr).Value;

                    CsLocal csLocal = new CsLocal(constExpr.Name);

                    _scopes.Define(csLocal, true, constExpr.Position);

                    CsInstructions.Add(new CsDeclare(csLocal, true, value));

                    return (csLocal, true);
                }

            case AssignExpr assignExpr:
                {
                    CsValue value = LowerExpr(assignExpr.Expr).Value;

                    CsLocal csLocal = _scopes.Set(assignExpr.Name, assignExpr.Position);

                    CsInstructions.Add(new CsAssign(csLocal, value));

                    return (csLocal, true);
                }

            case IfExpr ifExpr:
                {
                    CsLocal csLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(csLocal, false, new CsNil()));

                    // IF

                    CsValue condition = LowerExpr(ifExpr.Expr).Value;

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

                    return (csLocal, true);
                }

            case WhileExpr whileExpr:
                {
                    CsLocal csLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(csLocal, false, new CsNil()));

                    CsInstructions.Add(new CsWhileStart());

                    CsValue condition = LowerExpr(whileExpr.Expr).Value;

                    CsInstructions.Add(new CsWhileCondition(condition));

                    _scopes.BeginScope();

                    _loopStack.Push(csLocal);

                    CsValue whileLast = LowerExprs(whileExpr.Body);
                    CsInstructions.Add(new CsAssign(csLocal, whileLast));

                    _loopStack.Pop();

                    _scopes.EndScope();

                    CsInstructions.Add(new CsWhileEnd());

                    return (csLocal, true);
                }

            case BreakExpr breakExpr:
                {
                    CsValue csValue = breakExpr.Expr != null
                        ? LowerExpr(breakExpr.Expr).Value
                        : new CsNil();

                    CsLocal loopLocal = _loopStack.Peek();

                    CsInstructions.Add(new CsAssign(loopLocal, csValue));
                    CsInstructions.Add(new CsBreak());

                    return (csValue, true);
                }

            case ContinueExpr:
                CsInstructions.Add(new CsContinue());
                return (new CsNil(), true);

            case MatchExpr matchExpr:
                {
                    CsLocal resultLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(resultLocal, false, new CsNil()));

                    CsValue scrutinee = LowerExpr(matchExpr.Scutinee).Value;

                    CsLocal scrutineeLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(scrutineeLocal, false, scrutinee));

                    LowerMatchPatterns(scrutineeLocal, resultLocal, matchExpr.Patterns, 0);

                    return (resultLocal, true);
                }

            case BlockExpr blockExpr:
                {
                    CsLocal resultLocal = new CsLocal("lowerer_temp");

                    CsInstructions.Add(new CsDeclare(resultLocal, false, new CsNil()));

                    CsInstructions.Add(new CsBlockStart());

                    _scopes.BeginScope();

                    CsValue last = LowerExprs(blockExpr.Exprs);
                    CsInstructions.Add(new CsAssign(resultLocal, last));

                    _scopes.EndScope();

                    CsInstructions.Add(new CsBlockEnd());

                    return (resultLocal, true);
                }

            case FunctionExpr functionExpr:
                {
                    string functionName = $"actual_function_{_nextFunctionIndex++}";

                    CsInstructions.Add(new CsFunctionStart(functionName, functionExpr.Parameters.ToArray()));

                    _scopes.BeginScope();

                    CsLocal returnLocal = new CsLocal("generated_function_return");

                    CsInstructions.Add(new CsDeclare(returnLocal, false, new CsNil()));

                    _functionStack.Push(returnLocal);

                    for (int i = 0; i < functionExpr.Parameters.Count; i++)
                    {
                        string param = functionExpr.Parameters[i];

                        CsLocal paramLocal = new CsLocal(param);

                        _scopes.Define(paramLocal, false, functionExpr.Position);

                        CsInstructions.Add(new CsDeclare(paramLocal, false, new CsArgument(i)));
                    }

                    CsValue bodyResult = LowerExprs(functionExpr.Exprs);

                    CsInstructions.Add(new CsAssign(returnLocal, bodyResult));

                    CsInstructions.Add(new CsReturn(returnLocal));

                    _functionStack.Pop();

                    CsInstructions.Add(new CsFunctionEnd());

                    CsLocal functionLocal = new CsLocal("function_value");

                    CsInstructions.Add(new CsDeclare(functionLocal, false, new CsFunctionValue(functionName, functionExpr.Parameters.ToArray())));

                    return (functionLocal, true);
                }

            case ReturnExpr returnExpr:
                {
                    CsValue csValue = returnExpr.Expr != null
                        ? LowerExpr(returnExpr.Expr).Value
                        : new CsNil();

                    CsLocal returnLocal = _functionStack.Peek();

                    CsInstructions.Add(new CsAssign(returnLocal, csValue));
                    CsInstructions.Add(new CsReturn(returnLocal));

                    return (csValue, true);
                }

            case CallExpr callExpr:
                {
                    CsValue target = LowerExpr(callExpr.Target).Value;
                    List<CsValue> args = callExpr.Exprs
                        .Select(expr => LowerExpr(expr).Value)
                        .ToList();

                    return (new CsCall(target, args, callExpr.Position), false);
                }

            case ImportExpr importExpr:
                return (LowerFile(importExpr.Path, importExpr.Position), true);

            case IndexExpr indexExpr:
                {
                    CsValue target = LowerExpr(indexExpr.Target).Value;
                    CsValue index = LowerExpr(indexExpr.Index).Value;
                    return (new CsIndexGet(target, index, indexExpr.Position), false);
                }

            case IndexSetExpr indexSetExpr:
                {
                    IndexExpr indexExpr = indexSetExpr.IndexExpr;
                    CsValue target = LowerExpr(indexExpr.Target).Value;
                    CsValue index = LowerExpr(indexExpr.Index).Value;
                    CsValue value = LowerExpr(indexSetExpr.Value).Value;
                    return (new CsIndexSet(target, index, value, indexExpr.Position), false);
                }

            case MemberExpr memberExpr:
                {
                    CsValue target = LowerExpr(memberExpr.Target).Value;
                    return (new CsMemberGet(target, memberExpr.Member, memberExpr.Position), false);
                }

            case MemberSetExpr memberSetExpr:
                {
                    MemberExpr memberExpr = memberSetExpr.MemberExpr;
                    CsValue target = LowerExpr(memberExpr.Target).Value;
                    CsValue value = LowerExpr(memberSetExpr.Value).Value;
                    return (new CsMemberSet(target, memberExpr.Member, value, memberExpr.Position), false);
                }

            case FlowExpr flowExpr:
                {
                    CsValue left = LowerExpr(flowExpr.Input).Value;

                    CsLocal valueLocal = new CsLocal("value");

                    _scopes.BeginScope();

                    CsInstructions.Add(new CsDeclare(valueLocal, false, left));

                    _scopes.Define(valueLocal, false, flowExpr.Position);

                    CsValue target = LowerExpr(flowExpr.Target).Value;

                    _scopes.EndScope();

                    return (target, false);
                }

        }

        throw new Error($"'{expr.GetType().Name}' is an invalid expression", expr.Position);
    }

    CsValue LowerAnd(BinaryExpr binaryExpr)
    {
        CsLocal csLocal = new CsLocal("lowerer_temp");

        CsInstructions.Add(new CsDeclare(csLocal, false, new CsLiteral(false)));

        CsValue left = LowerExpr(binaryExpr.Left).Value;

        CsInstructions.Add(new CsIfStart(left));

        _scopes.BeginScope();

        CsValue right = LowerExpr(binaryExpr.Right).Value;

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

        CsValue left = LowerExpr(binaryExpr.Left).Value;

        CsInstructions.Add(new CsIfStart(left));
        CsInstructions.Add(new CsAssign(csLocal, new CsLiteral(true)));
        CsInstructions.Add(new CsIfEnd());

        CsInstructions.Add(new CsElseStart());

        _scopes.BeginScope();

        CsValue right = LowerExpr(binaryExpr.Right).Value;

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
            CsValue value = LowerExpr(matchDefault.Result).Value;
            CsInstructions.Add(new CsAssign(result, value));
            return;
        }

        MatchArm arm = (MatchArm)pattern;

        CsValue armPattern = LowerExpr(arm.Pattern).Value;

        CsValue condition = new CsBinary(
            TokenType.IsEqual,
            scrutinee,
            armPattern,
            arm.Pattern.Position
        );

        CsInstructions.Add(new CsIfStart(condition));

        _scopes.BeginScope();

        CsValue armResult = LowerExpr(arm.Result).Value;
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