using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Builtins
{
    record VectorObject(List<Value> Values) :
        ILueolDefaultEquality, ILueolDefaultToString, ILueolGetMember,
        ILueolGetIndex, ILueolSetIndex, ILueolName
    {
        public string Name => "vector";

        public bool GetMember(string name, out Value value)
        {
            switch (name)
            {
                case "length":
                    value = new Value(Values.Count);
                    return true;

                case "is_empty":
                    value = new Value(Values.Count == 0);
                    return true;

                case "first":
                    if (Values.Count == 0)
                        throw new InvalidOperationException("Cannot get the first item from an empty vector is empty");
                    value = Values[0];
                    return true;

                case "last":
                    if (Values.Count == 0)
                        throw new InvalidOperationException("Cannot get the last item from an empty vector is empty");
                    value = Values[^1];
                    return true;
            }

            if (!_memberFunctions.TryGetValue(name, out var function))
            {
                value = Value.Nil();
                return false;
            }

            value = new Value(function.Bind(new Value(this)));
            return true;
        }

        public void SetIndex(Value index, Value value)
        {
            int raw = index.ExpectKind(ValueKind.Int).AsInt32();
            Values[raw] = value;
        }

        public bool GetIndex(Value index, out Value value)
        {
            int raw = index.ExpectKind(ValueKind.Int).AsInt32();
            value = Values[raw];
            return true;
        }

        static Dictionary<string, Function> _memberFunctions = new Dictionary<string, Function>
        {
            {
                "push",
                Function.Normal("push", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    vector.Values.Add(args[0]);
                    return target!.Value;
                })
            },
            {
                "push_vector",
                Function.Normal("push_vector", ["other"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    var other = args[0].As<VectorObject>();
                    vector.Values.AddRange(other.Values);
                    return target!.Value;
                })
            },
            {
                "pop",
                Function.Normal("pop", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    if (vector.Values.Count == 0)
                        throw new InvalidOperationException("Cannot pop an item from an empty vector is empty");
                    Value value = vector.Values[^1];
                    vector.Values.RemoveAt(vector.Values.Count - 1);
                    return value;
                })
            },
            {
                "remove",
                Function.Normal("remove", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value needle = args[0];
                    for (int i = 0; i < vector.Values.Count; i++)
                    {
                        if (needle.Compare(vector.Values[i]))
                        {
                            vector.Values.RemoveAt(i);
                            break;
                        }
                    }
                    return target!.Value;
                })
            },
            {
                "remove_at",
                Function.Normal("remove_at", ["index"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = (int)args[0]
                        .ExpectKind(ValueKind.Int)
                        .AsInt();
                    vector.Values.RemoveAt(index);
                    return target!.Value;
                })
            },
            {
                "try_remove",
                Function.Normal("try_remove", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value needle = args[0];
                    for (int i = 0; i < vector.Values.Count; i++)
                    {
                        if (needle.Compare(vector.Values[i]))
                        {
                            vector.Values.RemoveAt(i);
                            return new Value(true);
                        }
                    }
                    return new Value(false);
                })
            },
            {
                "contains",
                Function.Normal("contains", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value needle = args[0];
                    for (int i = 0; i < vector.Values.Count; i++)
                    {
                        if (needle.Compare(vector.Values[i]))
                            return new Value(true);
                    }
                    return new Value(false);
                })
            },
            {
                "index_of",
                Function.Normal("index_of", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value needle = args[0];
                    for (int i = 0; i < vector.Values.Count; i++)
                    {
                        if (needle.Compare(vector.Values[i]))
                            return new Value(i);
                    }
                    return new Value(-1);
                })
            },
            {
                "insert",
                Function.Normal("insert", ["index", "item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = (int)args[0]
                        .ExpectKind(ValueKind.Int)
                        .AsInt();
                    Value value = args[1];
                    vector.Values.Insert(index, value);
                    return target.Value;
                })
            },
            {
                "insert_vector",
                Function.Normal("insert_vector", ["index", "other"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = (int)args[0]
                        .ExpectKind(ValueKind.Int)
                        .AsInt();
                    var value = args[1].As<VectorObject>();;
                    vector.Values.InsertRange(index, value.Values);
                    return target.Value;
                })
            },
            {
                "copy",
                Function.Normal("copy", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    return new Value(new VectorObject([..vector.Values]));
                })
            },
            {
                "reversed",
                Function.Normal("copy", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    var copy = new VectorObject([..vector.Values]);
                    copy.Values.Reverse();
                    return new Value(copy);
                })
            },
            {
                "reverse",
                Function.Normal("copy", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    vector.Values.Reverse();
                    return target.Value;
                })
            },
        };
    };
}