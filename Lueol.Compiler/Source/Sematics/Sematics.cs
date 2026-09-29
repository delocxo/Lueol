static class Sematics
{
    public static void Check(List<Expr> exprs)
    {
        foreach (Expr expr in exprs)
            CheckExpr(expr);
    }

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
        }
    }
}