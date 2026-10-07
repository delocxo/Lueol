using System.Collections.Frozen;

namespace Builtins
{
    static class VectorGlobals
    {
        static void X()
        {
        }
    }

    class VectorObject : List<Value>, ILueolName, ILueolToString, ILueolDefaultEquality,
        ILueolDefaultHash, ILueolGetMember, ILueolGetIndex, ILueolSetIndex
    {
        public VectorObject()
        {
        }

        public VectorObject(IEnumerable<Value> collection) : base(collection)
        {
        }

        public VectorObject(int capacity) : base(capacity)
        {
        }

        public string LueolName => "vector";

        public bool LueolGetIndex(Value index, out Value value)
        {
            int raw = index.ExpectInt32();
            value = this[raw];
            return true;
        }

        public bool LueolGetMember(string name, out Value value)
        {
            switch (name)
            {
                case "length":
                    value = new Value(Count);
                    return true;

                case "capacity":
                    value = new Value(Capacity);
                    return true;

                case "is_empty":
                    value = new Value(Count == 0);
                    return true;

                case "first":
                    if (Count == 0)
                        throw new InvalidOperationException("Cannot get the first item out of an empty vector");

                    value = new Value(this[0]);
                    return true;

                case "last":
                    if (Count == 0)
                        throw new InvalidOperationException("Cannot get the last item out of an empty vector");

                    value = new Value(this[^1]);
                    return true;
            }

            if (FunctionMembers.TryGetValue(name, out Function? function))
            {
                value = new Value(function.Bind(new Value(this)));
                return true;
            }

            value = Value.Nil();
            return false;
        }

        public void LueolSetIndex(Value index, Value value)
        {
            int raw = index.ExpectInt32();
            this[raw] = value;
        }

        public string LueolToString()
            => $"Vector({string.Join(
                    ", ",
                    this.Select(x => x.ToStringWithQuotes())
                )})";

        static FrozenDictionary<string, Function> FunctionMembers = new Dictionary<string, Function>
        {
            {
                "push",
                Function.Normal("push", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value item = args[0];
                    vector.Add(item);
                    return target.Value;
                })
            },
            {
                "pop",
                Function.Normal("pop", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    if (vector.Count == 0)
                        throw new InvalidOperationException("Cannot pop an empty vector");
                    Value last = vector[^1];
                    vector.RemoveAt(vector.Count - 1);
                    return last;
                })
            },
            {
                "push_vector",
                Function.Normal("push_vector", ["other_vector"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    var otherVector = args[0].As<VectorObject>();
                    vector.AddRange(otherVector);
                    return target.Value;
                })
            },
            {
                "clear",
                Function.Normal("clear", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    vector.Clear();
                    return target.Value;
                })
            },
            {
                "contains",
                Function.Normal("contains", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value item = args[0];

                    foreach (Value other in vector)
                        if (item.Compare(other))
                            return new Value(true);

                    return new Value(false);
                })
            },
            {
                "index_of",
                Function.Normal("index_of", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value item = args[0];

                    for (int i = 0; i < vector.Count; i++)
                        if (item.Compare(vector[i]))
                            return new Value(i);

                    return new Value(-1);
                })
            },
            {
                "ensure_capacity",
                Function.Normal("ensure_capacity", ["capacity"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int capacity = args[0].ExpectInt32();
                    return new Value(vector.EnsureCapacity(capacity));
                })
            },
            {
                "insert",
                Function.Normal("insert", ["index", "item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = args[0].ExpectInt32();
                    Value item = args[1];
                    vector.Insert(index, item);
                    return target.Value;
                })
            },
            {
                "insert_vector",
                Function.Normal("insert_vector", ["index", "other_vector"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = args[0].ExpectInt32();
                    var otherVector = args[1].As<VectorObject>();
                    vector.InsertRange(index, otherVector);
                    return target.Value;
                })
            },
            {
                "remove",
                Function.Normal("remove", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value item = args[0];
                    for (int i = 0; i < vector.Count; i++)
                        if (item.Compare(vector[i]))
                        {
                            vector.RemoveAt(i);
                            return new Value(true);
                        }

                    return new Value(false);
                })
            },
            {
                "remove_at",
                Function.Normal("remove_at", ["index"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = args[0].ExpectInt32();
                    vector.RemoveAt(index);
                    return target.Value;
                })
            },
            {
                "get_range",
                Function.Normal("get_range", ["index", "length"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = args[0].ExpectInt32();
                    int length = args[1].ExpectInt32();
                    return new Value(vector.GetRange(index, length));
                })
            },
            {
                "remove_range",
                Function.Normal("remove_range", ["index", "length"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    int index = args[0].ExpectInt32();
                    int length = args[1].ExpectInt32();
                    vector.RemoveRange(index, length);
                    return target.Value;
                })
            },
            {
                "reverse",
                Function.Normal("reverse", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    vector.Reverse();
                    return target.Value;
                })
            },
            {
                "copy",
                Function.Normal("copy", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    List<Value> copied = [.. vector];
                    return new Value(copied);
                })
            },
            {
                "trim_excess",
                Function.Normal("trim_excess", [], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    vector.TrimExcess();
                    return target.Value;
                })
            },
            {
                "try_push",
                Function.Normal("try_push", ["item"], (args, target) =>
                {
                    var vector = target!.Value.As<VectorObject>();
                    Value item = args[0];
                    for (int i = 0; i < vector.Count; i++)
                        if (item.Compare(vector[i]))
                            return new Value(false);
                    vector.Add(item);
                    return new Value(true);
                })
            },
        }.ToFrozenDictionary();
    }
}