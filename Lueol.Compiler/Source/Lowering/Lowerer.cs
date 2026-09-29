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
        var scope = _scopes.Peek();
        if (!scope.TryGetValue(name, out VariableSymbol? symbol))
            throw new Error($"'{name}' does not exist", position);
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

    public void LowerExprs(List<Expr> exprs)
    {
        foreach (Expr expr in exprs)
            LowerExpr(expr);
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

                    return value;
                }
        }

        throw new Error($"'{expr.GetType().Name}' is an invalid expression", expr.Position);
    }
}