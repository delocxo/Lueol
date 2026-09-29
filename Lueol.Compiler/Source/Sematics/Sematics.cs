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
        }
    }
}