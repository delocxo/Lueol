using System;
using System.Collections.Generic;
using System.Text;

record UseStmt(string Path, Position Position);

abstract record Expr(Position Position);
record IntExpr(long Value, Position Position) : Expr(Position);
record FloatExpr(double Value, Position Position) : Expr(Position);
record StringExpr(string Value, Position Position) : Expr(Position);
record BoolExpr(bool Value, Position Position) : Expr(Position);
record NilExpr(Position Position) : Expr(Position);
record NameExpr(string Name, Position Position) : Expr(Position);
record UnaryExpr(Expr Right, TokenType Op, Position Position) : Expr(Position);
record BinaryExpr(Expr Left, Expr Right, TokenType Op, Position Position) : Expr(Position);
record LetExpr(string Name, Expr Expr, Position Position) : Expr(Position);
record ConstExpr(string Name, Expr Expr, Position Position) : Expr(Position);
record AssignExpr(string Name, Expr Expr, Position Position) : Expr(Position);
record IfExpr(Expr Expr, List<Expr> IfBody, List<Expr>? ElseBody, Position Position) : Expr(Position);
record WhileExpr(Expr Expr, List<Expr> Body, Position Position) : Expr(Position);
record BreakExpr(Expr? Expr, Position Position) : Expr(Position);
record ContinueExpr(Position Position) : Expr(Position);

abstract record MatchPattern;
record MatchArm(Expr Pattern, Expr Result) : MatchPattern;
record MatchDefault(Expr Result) : MatchPattern;

record MatchExpr(Expr Scutinee, List<MatchPattern> Patterns, Position Position) : Expr(Position);
record BlockExpr(List<Expr> Exprs, Position Position) : Expr(Position);
record FunctionExpr(List<string> Parameters, List<Expr> Exprs, Position Position) : Expr(Position);
record ReturnExpr(Expr? Expr, Position Position) : Expr(Position);
record CallExpr(Expr Target, List<Expr> Exprs, Position Position) : Expr(Position);
record IndexExpr(Expr Target, Expr Index, Position Position) : Expr(Position);
record IndexSetExpr(IndexExpr IndexExpr, Expr Value) : Expr(IndexExpr.Position);
record MemberExpr(Expr Target, string Member, Position Position) : Expr(Position);
record MemberSetExpr(MemberExpr MemberExpr, Expr Value) : Expr(MemberExpr.Position);
record ImportExpr(string Path, Position Position) : Expr(Position);