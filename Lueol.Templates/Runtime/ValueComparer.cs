using System.Diagnostics.CodeAnalysis;

class ValueComparer : IEqualityComparer<Value>
{
    public bool Equals(Value x, Value y)
        => x.Compare(y);

    public int GetHashCode(Value obj)
        => obj.GetHashCode();
}