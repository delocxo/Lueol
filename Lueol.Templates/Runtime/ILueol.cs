using System.Collections.Frozen;
using System.Runtime.CompilerServices;

interface ILueolGetIndex
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LueolGetIndex(Value index, out Value value);
}

interface ILueolSetIndex
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LueolSetIndex(Value index, Value value);
}

interface ILueolGetMember
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LueolGetMember(string name, out Value value);
}

interface ILueolSetMember
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LueolSetMember(string name, Value value);
}

interface ILueolEquality
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LueolEquality(Value other);
}

interface ILueolDefaultEquality
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LueolDefaultEquality(Value other)
    {
        if (other.Kind != ValueKind.Object)
            return false;

        return Equals(this, other.Object);
    }
}

interface ILueolToString
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string LueolToString();
}

interface ILueolDefaultToString
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string LueolToString()
    {
        if (this is ILueolName lueolName)
            return $"<{lueolName.LueolName}>";
        return $"<{GetType().Name}>";
    }
}


interface ILueolIsTruthy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LueolIsTruthy();
}

interface ILueolName
{
    public string LueolName { get; }
}

interface ILueolHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LueolGetHash();
}

interface ILueolDefaultHash
{
    public int LueolGetDefaultHash()
        => GetHashCode();

}

interface ILueolCallable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value LueolCall(ReadOnlySpan<Value> arguments);
}