using System.Runtime.CompilerServices;

namespace Builtins
{

    static class IO
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

            Globals.AddFunction(Function.Normal("errprint", ["value"], (args, target) =>
            {
                Console.Error.WriteLine(args[0]);
                return Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("errprint_empty", [], (args, target) =>
            {
                Console.Error.WriteLine();
                return Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("errprint_with_ending", ["value", "ending"], (args, target) =>
            {
                Console.Error.Write(args[0]);
                Console.Error.Write(args[1]);
                return Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("input", [""], (args, target) =>
            {
                string? input = Console.ReadLine();
                return input != null ? new Value(input) : Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("prompt", ["prompt"], (args, target) =>
            {
                Console.Write(args[0]);
                string? input = Console.ReadLine();
                return input != null ? new Value(input) : Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("clear", [], (args, target) =>
            {
                Console.Clear();
                return Value.Nil();
            }));
        }
    }
}