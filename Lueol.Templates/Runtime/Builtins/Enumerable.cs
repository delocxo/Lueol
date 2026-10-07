using System.Collections;
using System.Collections.Frozen;

namespace Builtins
{
    static class Enumerable
    {

    }

    class EnumerableObject : IEnumerable<Value>
    {
        readonly IEnumerable<Value> _source;

        public EnumerableObject(IEnumerable<Value> source)
        {
            _source = source;
        }

        public IEnumerator<Value> GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static FrozenDictionary<string, Function> FunctionMembers = new Dictionary<string, Function>()
        {

        }.ToFrozenDictionary();
    }
}