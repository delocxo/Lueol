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
    int _instructionIndex = 0;

    public Emitter(Lowerer lowerer)
    {
        _instructions = lowerer.CsInstructions;
        _mangledNames = lowerer.MangledNames;
    }

    public void EmitInstructions()
    {
        while (_instructionIndex < _instructions.Count)
        {
            EmitInstruction(_instructions[_instructionIndex++]);
        }
    }

    public void EmitInstructionsUntil<T>() where T : CsInstruction
    {
        while (_instructionIndex < _instructions.Count)
        {
            CsInstruction instruction = _instructions[_instructionIndex++];
            if (instruction is T)
                break;
            EmitInstruction(instruction);
        }
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

            case CsIfStart csIfStart:
                {
                    string condition = EmitValue(csIfStart.Condition);

                    EmitLine($"if ({condition}.IsTruthy())");
                    EmitLine("{");

                    IncreaseIndent();

                    EmitInstructionsUntil<CsIfEnd>();

                    DecreaseIndent();
                    EmitLine("}");
                    break;
                }

            case CsElseIfStart csElseIfStart:
                {
                    string condition = EmitValue(csElseIfStart.Condition);

                    EmitLine($"else if ({condition}.IsTruthy())");
                    EmitLine("{");

                    IncreaseIndent();

                    EmitInstructionsUntil<CsElseIfEnd>();

                    DecreaseIndent();
                    EmitLine("}");
                    break;
                }

            case CsElseStart:
                {
                    EmitLine($"else");
                    EmitLine("{");

                    IncreaseIndent();

                    EmitInstructionsUntil<CsElseEnd>();

                    DecreaseIndent();
                    EmitLine("}");
                    break;
                }

            case CsWhileStart:
                {
                    EmitLine("while (true)");
                    EmitLine("{");
                    IncreaseIndent();
                    break;
                }

            case CsWhileCondition whileCondition:
                {
                    string condition = EmitValue(whileCondition.Condition);

                    EmitLine($"if (!{condition}.IsTruthy())");
                    IncreaseIndent();
                    EmitLine("break;");
                    DecreaseIndent();
                    break;
                }

            case CsWhileEnd:
                {
                    DecreaseIndent();
                    EmitLine("}");
                    break;
                }

            case CsBreak:
                EmitLine("break;");
                break;

            case CsContinue:
                EmitLine("continue;");
                break;

            case CsBlockStart:
                EmitLine("{");
                IncreaseIndent();
                break;

            case CsBlockEnd:
                DecreaseIndent();
                EmitLine("}");
                break;

            case CsFunctionStart csFunctionStart:
                {
                    EmitLine($"Value {csFunctionStart.GeneratedName}(ReadOnlySpan<Value> args, Value? target)");
                    EmitLine("{");
                    IncreaseIndent();

                    EmitInstructionsUntil<CsFunctionEnd>();

                    DecreaseIndent();
                    EmitLine("}");
                    break;
                }

            case CsReturn csReturn:
                {
                    string value = EmitValue(csReturn.CsValue);
                    EmitLine($"return {value};");
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
                        long l => NewValue($"{l.ToString(CultureInfo.InvariantCulture)}L"),
                        double d => NewValue($"{d.ToString(CultureInfo.InvariantCulture)}D"),
                        string s => NewValue($"\"{ToCSharpString(s.ToString(CultureInfo.InvariantCulture))}\""),
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

                    string tempName = GetTemp();

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

                        TokenType.IsEqual => "Equals",
                        TokenType.NotEqual => "NotEqual",

                        _ => throw new UnreachableException("Invalid binary operator")
                    };

                    string tempName = GetTemp();

                    EmitLine($"Value {tempName} = {left}.{function}({right});");

                    return tempName;
                }

            case CsArgument csArgument:
                return $"args[{csArgument.Index}]";

            case CsFunctionValue csFunctionValue:
                {
                    string function = $"Function.Anonymous([{string
                        .Join(", ", csFunctionValue
                        .Parameters
                        .Select(x => $"\"{x}\""))}], {csFunctionValue.GeneratedName})";
                    return NewValue(function);
                }

            case CsCall csCall:
                {
                    string target = EmitValue(csCall.Target);
                    string[] parameters = csCall.Values
                        .Select(EmitValue)
                        .ToArray();

                    EmitPosition(csCall.Position);

                    string tempName = GetTemp();

                    EmitLine($"Value {tempName} = {target}.Call([{string.Join(", ", parameters)}]);");

                    return tempName;
                }

            case CsGetGlobal csGetGlobal:
                {
                    string tempName = GetTemp();
                    EmitPosition(csGetGlobal.Position);
                    EmitLine($"Value {tempName} = Globals.GetGlobal(\"{csGetGlobal.Name}\");");
                    return tempName;
                }

            case CsIndexGet csIndexGet:
                {
                    string target = EmitValue(csIndexGet.Target);
                    string index = EmitValue(csIndexGet.Index);
                    string tempName = GetTemp();
                    EmitPosition(csIndexGet.Position);
                    EmitLine($"Value {tempName} = {target}.GetIndex({index});");
                    return tempName;
                }

            case CsIndexSet csIndexSet:
                {
                    string target = EmitValue(csIndexSet.Target);
                    string index = EmitValue(csIndexSet.Index);
                    string value = EmitValue(csIndexSet.Value);
                    string tempName = GetTemp();
                    EmitPosition(csIndexSet.Position);
                    EmitLine($"Value {tempName} = {target}.SetIndex({index}, {value});");
                    return tempName;
                }

            case CsMemberGet csMemberGet:
                {
                    string target = EmitValue(csMemberGet.Target);
                    string tempName = GetTemp();
                    EmitPosition(csMemberGet.Position);
                    EmitLine($"Value {tempName} = {target}.GetMember(\"{csMemberGet.Name}\");");
                    return tempName;
                }

            case CsMemberSet csMemberSet:
                {
                    string target = EmitValue(csMemberSet.Target);
                    string value = EmitValue(csMemberSet.Value);
                    string tempName = GetTemp();
                    EmitPosition(csMemberSet.Position);
                    EmitLine($"Value {tempName} = {target}.SetMember(\"{csMemberSet.Name}\", {value});");
                    return tempName;
                }
        }

        throw new UnreachableException($"");
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

    void IncreaseIndent() => _indentLevel++;
    void DecreaseIndent() => _indentLevel--;

    string GetTemp() => $"emitter_temp_{_nextTempIndex++}";
}