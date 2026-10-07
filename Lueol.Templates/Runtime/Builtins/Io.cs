using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Io
    {
        [ModuleInitializer]
        public static void Init()
        {
            var io = Globals.AddNamespace("io");

            io.AddFunction(Function.Normal("print", ["value"], (args, target) =>
            {
                Value value = args[0];
                Console.Write(value);
                return Value.Nil();
            }));

            io.AddFunction(Function.Normal("println", ["value"], (args, target) =>
            {
                Value value = args[0];
                Console.WriteLine(value);
                return Value.Nil();
            }));

            io.AddFunction(Function.Normal("input", [], (args, target) =>
            {
                string? input = Console.ReadLine();
                return input != null ? new Value(input) : Value.Nil();
            }));

            io.AddFunction(Function.Normal("prompt", ["prompt"], (args, target) =>
            {
                string prompt = args[0].ToString();
                Console.Write(prompt);
                string? input = Console.ReadLine();
                return input != null ? new Value(input) : Value.Nil();
            }));

            io.AddFunction(Function.Normal("eprint", ["value"], (args, target) =>
            {
                Value value = args[0];
                Console.Error.Write(value);
                return Value.Nil();
            }));

            io.AddFunction(Function.Normal("eprintln", ["value"], (args, target) =>
            {
                Value value = args[0];
                Console.Error.WriteLine(value);
                return Value.Nil();
            }));
        }
    }
}