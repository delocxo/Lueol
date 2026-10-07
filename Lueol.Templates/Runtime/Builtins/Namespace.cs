using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Builtins
{

    static class Namespace
    {
        [ModuleInitializer]
        public static void Init()
        {
            Globals.AddFunction(Function.Normal("namespace", ["members"], (args, pos) =>
            {
                var vector = args[0].As<VectorObject>();
                var ns = new NamespaceObject(vector.Count);

                foreach (Value item in vector)
                {
                    var member = item.As<VectorObject>();
                    if (member.Count != 2)
                        throw new InvalidOperationException("Expected a name and value");
                    string name = member[0].ToString();
                    Value value = member[1];
                    ns.Add(name, value);
                }

                return new Value(ns);
            }));
        }
    }

    class NamespaceObject : Dictionary<string, Value>, ILueolName, ILueolToString,
        ILueolDefaultEquality, ILueolDefaultHash, ILueolGetMember, ILueolGetIndex
    {
        public NamespaceObject()
        {
        }

        public NamespaceObject(IDictionary<string, Value> dictionary) : base(dictionary)
        {
        }

        public NamespaceObject(IDictionary<string, Value> dictionary, IEqualityComparer<string>? comparer) : base(dictionary, comparer)
        {
        }

        public NamespaceObject(IEnumerable<KeyValuePair<string, Value>> collection) : base(collection)
        {
        }

        public NamespaceObject(IEnumerable<KeyValuePair<string, Value>> collection, IEqualityComparer<string>? comparer) : base(collection, comparer)
        {
        }

        public NamespaceObject(IEqualityComparer<string>? comparer) : base(comparer)
        {
        }

        public NamespaceObject(int capacity) : base(capacity)
        {
        }

        public NamespaceObject(int capacity, IEqualityComparer<string>? comparer) : base(capacity, comparer)
        {
        }

        protected NamespaceObject(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public string LueolName => "namespace";

        public bool LueolGetIndex(Value index, out Value value)
        {
            string raw = index.ToString();
            value = this[raw];
            return true;
        }

        public bool LueolGetMember(string name, out Value value)
            => TryGetValue(name, out value);

        public string LueolToString()
            => $"Namespace({string.Join(
                    ", ",
                    this.Select(x =>
                        $"{x.Key} = {x.Value.ToStringWithQuotes()}"
                    ))})";

        public void AddFunction(Function function)
        {
            if (string.IsNullOrWhiteSpace(function.Name) || function.IsAnonymous)
                throw new InvalidOperationException("Function cannot be anonymous or have an empty name");
            this[function.Name] = new Value(function);
        }

        public NamespaceObject AddNamespace(string name, NamespaceObject namespaceObject)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Namespace cannot have an empty name");
            this[name] = new Value(namespaceObject);
            return namespaceObject;
        }
    }
}