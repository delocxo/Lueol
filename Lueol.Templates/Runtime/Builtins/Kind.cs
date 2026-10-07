using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Kind
    {
        [ModuleInitializer]
        public static void Init()
        {
            var kind = Globals.AddNamespace("kind");

            kind.AddFunction(Function.Normal("is", ["value", "kind"], (args, target) =>
            {
                Value value = args[0];
                string kind = args[1].ExpectString();
                return new Value(value.GetName() == kind);
            }));

            kind.AddFunction(Function.Normal("kind", ["value"], (args, target) =>
            {
                Value value = args[0];
                return new Value(value.GetName());
            }));

            kind.AddFunction(Function.Normal("expect", ["value", "kind"], (args, target) =>
            {
                Value value = args[0];
                string kind = args[1].ExpectString();
                if (value.GetName() != kind)
                    throw new InvalidKindException($"Expected '{kind}', got {value.GetNameInQuotes()}");
                return value;
            }));
        }
    }
}