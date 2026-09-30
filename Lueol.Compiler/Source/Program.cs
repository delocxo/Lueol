if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: Lueol <file.lueol>");
    return 1;
}

bool optimize = args.Length > 1 && args[1] == "-o";

Lowerer lowerer = new Lowerer();
CsValue last = lowerer.LowerFile(args[0], null);
if (lowerer.CsInstructions.Count == 0 || lowerer.CsInstructions[^1] is not CsReturn)
{
    lowerer.CsInstructions.Add(new CsReturn(last));
}
lowerer.MangleNames();
// foreach (var instruction in lowerer.CsInstructions)
//     Console.WriteLine(instruction);

Emitter emitter = new Emitter(lowerer, optimize);
emitter.EmitInstructions();
// Console.WriteLine(emitter.Result);

SourceGenerator sourceGenerator = new SourceGenerator(
    emitter.Result,
    lowerer.UseStmts,
    emitter.PositionsCache
);
string result = sourceGenerator.Generate(optimize);
File.WriteAllText("generated.cs", result);
// Console.WriteLine(result);

return 0;