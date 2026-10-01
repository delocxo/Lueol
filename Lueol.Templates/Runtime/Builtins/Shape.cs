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

    record ShapeObject(Dictionary<string, Value> Members) :
        ILueolDefaultEquality, ILueolDefaultToString, ILueolGetMember,
        ILueolSetMember, ILueolName, ILueolGetIndex
    {
        public string Name => "shape";

        public static ShapeObject Create(ReadOnlySpan<(string Name, Value Value)> values)
        {
            Dictionary<string, Value> members = [];
            foreach (var pair in values)
                if (!members.TryAdd(pair.Name, pair.Value))
                    throw new DuplicateNameException($"'{pair.Name}' is a duplicate field");
            return new ShapeObject(members);
        }

        public static ShapeObject CreateResult(bool success, Value value)
            => Create([
                ("success", new Value(true)),
                ("value", value)
            ]);

        public bool GetMember(string name, out Value value)
        {
            if (_memberFunctions.TryGetValue(name, out var function))
            {
                value = new Value(function.Bind(new Value(this)));
                return true;
            }

            if (!Members.TryGetValue(name, out value))
                return false;

            return true;
        }

        public void SetMember(string name, Value value)
        {
            if (!Members.ContainsKey(name))
                throw new UnknownMemberException($"'{name}' is not a valid member of shape");
            Members[name] = value;
        }

        public bool GetIndex(Value index, out Value value)
        {
            string name = index.ExpectKind(ValueKind.String).AsString();
            if (!Members.TryGetValue(name, out value))
            {
                value = Value.Nil();
                return false;
            }
            return true;
        }

        static Dictionary<string, Function> _memberFunctions = new Dictionary<string, Function>
        {
            {
                "add",
                Function.Normal("add", ["name", "value"], (args, target) =>
                {
                    var shape = target!.Value.As<ShapeObject>();
                    string name = args[0].ExpectKind(ValueKind.String).AsString();
                    if (!shape.Members.TryAdd(name, args[1]))
                        throw new DuplicateNameException($"'{name}' is a duplicate field");
                    return target.Value;
                })
            }
        };
    };
}