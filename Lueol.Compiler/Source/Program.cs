if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: Lueol <file.lueol>");
    return 1;
}

if (!File.Exists(args[0]))
{
    Console.Error.WriteLine($"File '{args[0]}' does not exist");
    return 1;
}

List<Token> tokens = new Lexer(File.ReadAllText(args[0]), args[0]).Lex();
Parser parser = new Parser(tokens);
List<Expr> exprs = parser.Parse();
Sematics.Check(exprs);

Lowerer lowerer = new Lowerer();
CsValue last = lowerer.LowerExprs(exprs);
if (lowerer.CsInstructions.Count == 0 || lowerer.CsInstructions[^1] is not CsReturn)
{
    lowerer.CsInstructions.Add(new CsReturn(last));
}
lowerer.MangleNames();
// foreach (var instruction in lowerer.CsInstructions)
//     Console.WriteLine(instruction);

Emitter emitter = new Emitter(lowerer);
emitter.EmitInstructions();
// Console.WriteLine(emitter.Result);

SourceGenerator sourceGenerator = new SourceGenerator(emitter.Result);
string result = sourceGenerator.Generate();
File.WriteAllText("generated.cs", result);
// Console.WriteLine(result);

return 0;