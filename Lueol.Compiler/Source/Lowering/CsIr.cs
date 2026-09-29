using System.Diagnostics;

abstract record CsValue
{
    public override string ToString() => this switch
    {
        CsLiteral x => x.Value.ToString()!,
        CsLocal x => x.Name,
        CsNil => "nil",
        CsUnary x => $"{x.Op} {x.Right}",
        CsBinary x => $"({x.Left} {x.Op} {x.Right})",
        _ => throw new UnreachableException()
    };
}

record CsLiteral(object Value) : CsValue;
record CsLocal(string Name) : CsValue;
record CsNil() : CsValue;
record CsUnary(TokenType Op, CsValue Right, Position Position) : CsValue;
record CsBinary(TokenType Op, CsValue Left, CsValue Right, Position Position) : CsValue;


abstract record CsInstruction
{
    public override string ToString() => this switch
    {
        CsDeclare x => $"Declare {x.CsLocal.Name} = {x.Value}",
        CsAssign x => $"Assign {x.CsLocal.Name} = {x.Value}",
        CsPosition x => $"Position {x.Position}",
        _ => throw new UnreachableException()
    };
}

record CsDeclare(CsLocal CsLocal, bool IsConst, CsValue Value) : CsInstruction;
record CsAssign(CsLocal CsLocal, CsValue Value) : CsInstruction;
record CsPosition(Position Position) : CsInstruction;
record CsIfStart(CsValue Condition) : CsInstruction;
record CsElseStart : CsInstruction;
record CsElseEnd : CsInstruction;
record CsIfEnd : CsInstruction;