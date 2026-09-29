using System.Diagnostics;
using System.Globalization;
using System.Text;

class Emitter
{
    public StringBuilder Result { get; } = new StringBuilder();

    List<CsInstruction> _instructions;
    Dictionary<CsLocal, string> _mangledNames;
    long _nextTempIndex = 0;
    int _indentLevel = 1;
    int _indentSize = 4;

    public Emitter(Lowerer lowerer)
    {
        _instructions = lowerer.CsInstructions;
        _mangledNames = lowerer.MangledNames;
    }

    public void EmitInstructions()
    {
        foreach (var instruction in _instructions)
            EmitInstruction(instruction);
    }

    public void EmitInstruction(CsInstruction instruction)
    {
        switch (instruction)
        {
            case CsDeclare csDeclare:
                {
                    string value = EmitValue(csDeclare.Value);
                    EmitLine(csDeclare.IsConst ? "// Constant" : "// Non-Constant");
                    EmitLine($"Value {_mangledNames[csDeclare.CsLocal]} = {value};");
                    break;
                }

            case CsAssign csAssign:
                {
                    string value = EmitValue(csAssign.Value);
                    EmitLine($"{_mangledNames[csAssign.CsLocal]} = {value};");
                    break;
                }

            case CsPosition csPosition:
                {
                    EmitPosition(csPosition.Position);
                    break;
                }
        }
    }

    string EmitValue(CsValue csValue)
    {
        switch (csValue)
        {
            case CsLiteral csLiteral:
                {
                    return csLiteral.Value switch
                    {
                        long l => NewValue(l.ToString(CultureInfo.InvariantCulture)),
                        double d => NewValue(d.ToString(CultureInfo.InvariantCulture)),
                        string s => NewValue($"{ToCSharpString(s.ToString(CultureInfo.InvariantCulture))}"),
                        bool b => NewValue(b ? "true" : "false"),
                        _ => throw new UnreachableException()
                    };
                }

            case CsNil:
                return "Value.Nil()";

            case CsLocal csLocal:
                return _mangledNames[csLocal];

            case CsUnary csUnary:
                {
                    string right = EmitValue(csUnary.Right);

                    EmitPosition(csUnary.Position);

                    string function;
                    if (csUnary.Op == TokenType.Bang)
                        function = $"{right}.Flip()";
                    else
                        function = $"{right}.Negate()";

                    string tempName = $"temp_{_nextTempIndex++}";

                    EmitLine($"Value {tempName} = {function};");

                    return tempName;
                }

            case CsBinary csBinary:
                {
                    string left = EmitValue(csBinary.Left);
                    string right = EmitValue(csBinary.Right);

                    EmitPosition(csBinary.Position);

                    string function = csBinary.Op switch
                    {
                        TokenType.Add => "Add",
                        TokenType.Sub => "Sub",
                        TokenType.Mul => "Mul",
                        TokenType.Div => "Div",
                        TokenType.Mod => "Mod",

                        TokenType.Less => "Less",
                        TokenType.Greater => "Greater",
                        TokenType.LessEq => "LessEq",
                        TokenType.GreaterEq => "GreaterEq",

                        _ => throw new UnreachableException()
                    };

                    string tempName = $"temp_{_nextTempIndex++}";

                    EmitLine($"Value {tempName} = {left}.{function}({right});");

                    return tempName;
                }
        }

        throw new UnreachableException();
    }

    string NewValue(string value) => $"new Value({value})";

    void EmitLine(string text)
    {
        string padding = new string(' ', _indentLevel * _indentSize);

        string[] lines = text
            .ReplaceLineEndings("\n")
            .Split("\n");

        foreach (string line in lines)
        {
            Result.Append(padding);
            Result.AppendLine(line);
        }
    }

    string ToCSharpString(string str) => str
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\t", "\\t")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r")
        .Replace("\f", "\\f")
        .Replace("\a", "\\a")
        .Replace("\b", "\\b")
        .Replace("\e", "\\e")
        .Replace("\v", "\\v")
        .Replace("\0", "\\0");

    void EmitPosition(Position position)
    {
        EmitLine($"RuntimeState.Position = new Position({position.Line}, {position.Column}, \"{ToCSharpString(position.Source)}\");");
    }
}