using System.Runtime.CompilerServices;

static class Builtins
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Globals.AddFunction(Function.Normal("print", ["value"], (args, target) =>
        {
            Console.WriteLine(args[0]);
            return Value.Nil();
        }));

        Globals.AddFunction(Function.Normal("print_empty", [], (args, target) =>
        {
            Console.WriteLine();
            return Value.Nil();
        }));

        Globals.AddFunction(Function.Normal("print_with_ending", ["value", "ending"], (args, target) =>
        {
            Console.Write(args[0]);
            Console.Write(args[1]);
            return Value.Nil();
        }));
    }
}