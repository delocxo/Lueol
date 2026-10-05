using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Builtins
{

    static class Map
    {
        [ModuleInitializer]
        public static void Init()
        {
            Globals.AddFunction(Function.Normal("map", [], (args, pos) =>
            {
                return new Value(new MapObject());
            }));
        }
    }

    class MapObject : Dictionary<Value, Value>, ILueolName, ILueolToString,
        ILueolDefaultEquality, ILueolDefaultHash, ILueolGetMember, ILueolGetIndex, ILueolSetIndex
    {
        public MapObject() : base(new ValueComparer())
        {
        }

        public MapObject(IDictionary<Value, Value> dictionary)
            : base(dictionary, new ValueComparer())
        {
        }

        public MapObject(IEnumerable<KeyValuePair<Value, Value>> collection)
            : base(collection, new ValueComparer())
        {
        }

        public MapObject(int capacity)
            : base(capacity, new ValueComparer())
        {
        }

        public string LueolName => throw new NotImplementedException();

        public bool LueolGetIndex(Value index, out Value value)
        {
            value = this[index];
            return true;
        }

        public bool LueolGetMember(string name, out Value value)
        {
            switch (name)
            {
                case "capacity":
                    value = new Value(Capacity);
                    return true;

                case "count":
                    value = new Value(Count);
                    return true;

                case "keys":
                    {
                        var keys = new VectorObject(Keys);
                        value = new Value(keys);
                        return true;
                    }

                case "values":
                    {
                        var values = new VectorObject(Values);
                        value = new Value(values);
                        return true;
                    }
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
            this[index] = value;
        }

        public string LueolToString()
            => $"Map({string.Join(
                    ", ",
                    this.Select(x =>
                        $"{x.Key.ToStringWithQuotes()}: {x.Value.ToStringWithQuotes()}"
                    ))})";

        public static MapObject CreateResult(bool success, Value result)
            => new MapObject()
            {
                [new Value("success")] = new Value(success),
                [new Value("result")] = result
            };

        static FrozenDictionary<string, Function> FunctionMembers = new Dictionary<string, Function>
        {
            {
                "add",
                Function.Normal("add", ["key", "value"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value key = args[0];
                    Value value = args[1];
                    map.Add(key, value);
                    return target.Value;
                })
            },
            {
                "remove",
                Function.Normal("remove", ["key"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value key = args[0];
                    return new Value(map.Remove(key));
                })
            },
            {
                "clear",
                Function.Normal("clear", [], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    map.Clear();
                    return target.Value;
                })
            },
            {
                "contains_key",
                Function.Normal("contains_key", ["key"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value key = args[0];
                    return new Value(map.ContainsKey(key));
                })
            },
            {
                "contains_value",
                Function.Normal("contains_value", ["value"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value value = args[0];
                    return new Value(map.ContainsValue(value));
                })
            },
            {
                "ensure_capacity",
                Function.Normal("ensure_capacity", ["capacity"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    int capacity = args[0].ExpectInt32();
                    return new Value(map.EnsureCapacity(capacity));
                })
            },
            {
                "trim_excess",
                Function.Normal("trim_excess", [], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    map.TrimExcess();
                    return target.Value;
                })
            },
            {
                "try_add",
                Function.Normal("try_add", ["key", "value"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value key = args[0];
                    Value value = args[1];
                    return new Value(map.TryAdd(key, value));
                })
            },
            {
                "try_get_value",
                Function.Normal("try_get_value", ["key"], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    Value key = args[0];
                    bool success = map.TryGetValue(key, out Value result);
                    return new Value(CreateResult(success, result));
                })
            },
            {
                "reverse",
                Function.Normal("reverse", [], (args, target) =>
                {
                    var map = target!.Value.As<MapObject>();
                    int count = map.Count;
                    var kvpArray = new KeyValuePair<Value, Value>[count];
                    int index = 0;
                    foreach (var kvp in map)
                        kvpArray[index++] = kvp;
                    map.Clear();
                    for (int i = count - 1; i >= 0; i--)
                        map.Add(kvpArray[i].Key, kvpArray[i].Value);
                    return target.Value;
                })
            },
        }.ToFrozenDictionary();
    }
}