static class Sematics
{
    public static void Check(List<Expr> exprs)
    {
        foreach (Expr expr in exprs)
            CheckExpr(expr);
    }

    static int _loopDepth = 0;

    static void CheckExpr(Expr expr)
    {
        switch (expr)
        {
            case UnaryExpr unaryExpr:
                CheckExpr(unaryExpr.Right);
                break;

            case BinaryExpr binaryExpr:
                CheckExpr(binaryExpr.Left);
                CheckExpr(binaryExpr.Right);
                break;

            case LetExpr letExpr:
                CheckExpr(letExpr.Expr);
                break;

            case ConstExpr constExpr:
                CheckExpr(constExpr.Expr);
                break;

            case AssignExpr assignExpr:
                CheckExpr(assignExpr.Expr);
                break;

            case IfExpr ifExpr:
                {
                    CheckExpr(ifExpr.Expr);
                    Check(ifExpr.IfBody);
                    if (ifExpr.ElseBody != null)
                        Check(ifExpr.ElseBody);
                    break;
                }

            case WhileExpr whileExpr:
                {
                    CheckExpr(whileExpr.Expr);
                    _loopDepth++;
                    Check(whileExpr.Body);
                    _loopDepth--;
                    break;
                }

            case BreakExpr breakExpr:
                {
                    if (_loopDepth <= 0)
                        throw new Error("Break cannot be used outside a loop", breakExpr.Position);

                    if (breakExpr.Expr != null)
                        CheckExpr(breakExpr.Expr);

                    break;
                }

            case ContinueExpr continueExpr:
                if (_loopDepth <= 0)
                    throw new Error("Continue cannot be used outside a loop", continueExpr.Position);
                break;

            case MatchExpr matchExpr:
                {
                    CheckExpr(matchExpr.Scutinee);

                    bool hasDefault = false;

                    for (int i = 0; i < matchExpr.Patterns.Count; i++)
                    {
                        var pattern = matchExpr.Patterns[i];

                        if (pattern is MatchArm matchArm)
                        {
                            CheckExpr(matchArm.Pattern);
                            CheckExpr(matchArm.Result);
                        }
                        else if (pattern is MatchDefault matchDefault)
                        {
                            if (hasDefault)
                                throw new Error("Can only have one default match", matchExpr.Position);

                            if (i != matchExpr.Patterns.Count - 1)
                                throw new Error("Default match must be last", matchExpr.Position);

                            hasDefault = true;

                            CheckExpr(matchDefault.Result);
                        }
                    }

                    break;
                }

            case BlockExpr blockExpr:
                Check(blockExpr.Exprs);
                break;

            case FunctionExpr functionExpr:
                {
                    HashSet<string> parameters = [];

                    foreach (var param in functionExpr.Parameters)
                        if (!parameters.Add(param))
                            throw new Error($"'{param}' is a duplicate function parameter", functionExpr.Position);

                    Check(functionExpr.Exprs);
                    break;
                }

            case ReturnExpr returnExpr:
                {
                    if (returnExpr.Expr != null)
                        CheckExpr(returnExpr.Expr);

                    break;
                }

            case CallExpr callExpr:
                {
                    CheckExpr(callExpr.Target);
                    foreach (Expr arg in callExpr.Exprs)
                        CheckExpr(arg);
                    break;
                }

            case IndexExpr indexExpr:
                CheckExpr(indexExpr.Target);
                CheckExpr(indexExpr.Index);
                break;

            case IndexSetExpr indexSetExpr:
                CheckExpr(indexSetExpr.IndexExpr);
                CheckExpr(indexSetExpr.Value);
                break;

            case MemberExpr memberExpr:
                CheckExpr(memberExpr.Target);
                break;

            case MemberSetExpr memberSetExpr:
                CheckExpr(memberSetExpr.MemberExpr);
                CheckExpr(memberSetExpr.Value);
                break;
        }
    }
}