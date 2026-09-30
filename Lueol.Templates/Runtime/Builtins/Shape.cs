using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Shape
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            Globals.AddFunction(Function.Normal("shape_new", [], (args, pos) =>
            {
                return new Value(new ShapeObject([]));
            }));
        }
    }

    record ShapeObject(Dictionary<string, Value> Members) : ILueolDefaultEquality, ILueolToString, ILueolGetMember
    {
        public static ShapeObject Create(ReadOnlySpan<(string Name, Value Value)> values)
        {
            Dictionary<string, Value> members = [];
            foreach (var pair in values)
                if (!members.TryAdd(pair.Name, pair.Value))
                    throw new DuplicateNameException($"'{pair.Name}' is a duplicate field");
            return new ShapeObject(members);
        }

        public bool GetMember(string name, out Value value)
        {
            if (_memberFunctions.TryGetValue(name, out var function))
            {
                value = new Value(function.Bind(new Value(this)));
                return true;
            }

            if (!Members.TryGetValue(name, out value))
                throw new UnknownMemberException($"'{name}' is not a valid member of shape");

            return true;
        }

        public string ToLueolToString()
            => "<shape>";

        static Dictionary<string, Function> _memberFunctions = new Dictionary<string, Function>
        {
            {
                "add",
                Function.Normal("add", ["name", "value"], (args, target) =>
                {
                    var shape = target!.Value.As<ShapeObject>();
                    string name = args[0].ToString();
                    if (!shape.Members.TryAdd(name, args[1]))
                        throw new DuplicateNameException($"'{name}' is a duplicate field");
                    return target.Value;
                })
            }
        };
    };
}