using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;

enum ValueKind : byte
{
    Int,
    Float,
    String,
    Bool,
    Nil,
    Function,
    Object
}

static class ValueKindNames
{
    static readonly string[] s_names = CacheEnumNames();

    static string[] CacheEnumNames()
    {
        var values = (ValueKind[])Enum.GetValuesAsUnderlyingType(typeof(ValueKind));
        var result = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
            result[i] = values[i]
                .ToString()
                .ToLowerInvariant();

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetName(ValueKind kind)
    {
        byte index = (byte)kind;
        return s_names[index];
    }
}

readonly struct Value
{
    public readonly ulong Payload;
    public readonly object? Object;
    public readonly ValueKind Kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(long value)
    {
        Kind = ValueKind.Int;
        Payload = unchecked((ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(double value)
    {
        Kind = ValueKind.Float;
        Payload = BitConverter.DoubleToUInt64Bits(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(string value)
    {
        Kind = ValueKind.String;
        Object = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(bool value)
    {
        Kind = ValueKind.Bool;
        Payload = value ? 1U : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(Function function)
    {
        Kind = ValueKind.Function;
        Object = function;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(ValueKind kind)
    {
        Kind = kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(object value)
    {
        Kind = ValueKind.Object;
        Object = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Nil() => new Value(ValueKind.Nil);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long AsInt() => unchecked((long)Payload);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AsInt32() => unchecked((int)(long)Payload);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AsFloat() => BitConverter.UInt64BitsToDouble(Payload);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AsNumber()
    {
        return Kind switch
        {
            ValueKind.Float => BitConverter.UInt64BitsToDouble(Payload),
            ValueKind.Int => unchecked((long)Payload),
            _ => throw new InvalidKindException($"'{GetNameInQuotes()}' is not a valid numeric kind")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string AsString() => (string)Object!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AsBool() => Payload != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Function AsFunction() => (Function)Object!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T As<T>()
    {
        if (Object is not T obj)
            throw new InvalidKindObjectException($"Expected '{nameof(T)}'");
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAs<T>(out T? value)
    {
        if (Object is not T obj)
        {
            value = default;
            return false;
        }
        value = obj;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKind(ValueKind kind) => Kind == kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value ExpectKind(ValueKind kind)
    {
        if (Kind != kind)
            throw new InvalidKindException($"Expected {ValueKindNames.GetName(kind)}, got {GetNameInQuotes()}");

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ExpectString()
    {
        ExpectKind(ValueKind.String);
        return AsString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ExpectInt()
    {
        ExpectKind(ValueKind.Int);
        return AsInt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ExpectInt32()
    {
        ExpectKind(ValueKind.Int);
        return AsInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ExpectFloat()
    {
        ExpectKind(ValueKind.Float);
        return AsFloat();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ExpectFloat32()
    {
        ExpectKind(ValueKind.Float);
        return (float)AsFloat();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExpectBool()
    {
        ExpectKind(ValueKind.Bool);
        return AsBool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Function ExpectFunction()
    {
        ExpectKind(ValueKind.Function);
        return AsFunction();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNumber()
    {
        return Kind switch
        {
            ValueKind.Int => true,
            ValueKind.Float => true,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetName()
    {
        if (Kind != ValueKind.Object)
            return ValueKindNames.GetName(Kind);

        else if (Object is ILueolName lueolName)
            return lueolName.LueolName;

        return Object!.GetType().Name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetNameInQuotes() => $"'{GetName()}'";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTruthy()
    {
        return Kind switch
        {
            ValueKind.Bool => Payload != 0,
            ValueKind.Nil => false,
            _ => ObjectIsTruthy()
        };
    }

    public bool ObjectIsTruthy()
    {
        if (Object is ILueolIsTruthy lueolIsTruthy)
            return lueolIsTruthy.LueolIsTruthy();

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Compare(Value other)
    {
        return (Kind, other.Kind) switch
        {
            (ValueKind.Int, ValueKind.Int) => AsInt() == other.AsInt(),
            (ValueKind.Float, ValueKind.Int) => AsFloat() == other.AsInt(),
            (ValueKind.Int, ValueKind.Float) => AsInt() == other.AsFloat(),
            (ValueKind.Float, ValueKind.Float) => AsFloat() == other.AsFloat(),
            (ValueKind.String, ValueKind.String) => string.Equals(AsString(), other.AsString(), StringComparison.Ordinal),
            (ValueKind.Bool, ValueKind.Bool) => AsBool() == other.AsBool(),
            (ValueKind.Nil, ValueKind.Nil) => true,
            (ValueKind.Function, ValueKind.Function) => AsFunction() == other.AsFunction(),
            _ => CompareObject(other)
        };
    }

    public bool CompareObject(Value other)
    {
        if (Object is ILueolEquality lueolEquality)
            return lueolEquality.LueolEquality(other);

        else if (Object is ILueolDefaultEquality lueolDefaultEquality)
            return lueolDefaultEquality.LueolDefaultEquality(other);

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Equals(Value other)
        => new Value(Compare(other));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value NotEqual(Value other)
    => new Value(!Compare(other));

    public override string ToString()
    {
        return Kind switch
        {
            ValueKind.Int => AsInt().ToString(CultureInfo.InvariantCulture),
            ValueKind.Float => AsFloat().ToString(CultureInfo.InvariantCulture),
            ValueKind.String => AsString(),
            ValueKind.Bool => AsBool() ? "true" : "false",
            ValueKind.Nil => "nil",
            ValueKind.Function => AsFunction().ToString(),
            _ => ObjectToString()
        };
    }

    public string ObjectToString()
    {
        if (Object is ILueolToString lueolToString)
            return lueolToString.LueolToString();

        else if (Object is ILueolDefaultToString lueolDefaultToString)
            return lueolDefaultToString.LueolToString();

        throw new InvalidKindException($"{GetNameInQuotes()} cannot be converted into a string");
    }

    /// <summary>
    /// Basically the normal ToString function but it wraps only the string in quotes
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToStringWithQuotes()
    {
        if (IsKind(ValueKind.String))
            return $"'{AsString()}'";
        return ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Add(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() + other.AsNumber());

            return new Value(unchecked(AsInt() + other.AsInt()));
        }

        else if (IsKind(ValueKind.String) || other.IsKind(ValueKind.String))
            return new Value(ToString() + other.ToString());

        throw BinaryException.Binary(this, other, "+");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Sub(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() - other.AsNumber());

            return new Value(unchecked(AsInt() - other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "-");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Mul(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() * other.AsNumber());

            return new Value(unchecked(AsInt() * other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "*");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Div(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() / other.AsNumber());

            return new Value(unchecked(AsInt() / other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "/");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Mod(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() % other.AsNumber());

            return new Value(unchecked(AsInt() % other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "%");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Less(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() < other.AsNumber());

            return new Value(unchecked(AsInt() < other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "<");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Greater(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() > other.AsNumber());

            return new Value(unchecked(AsInt() > other.AsInt()));
        }

        throw BinaryException.Binary(this, other, ">");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value LessEq(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() <= other.AsNumber());

            return new Value(unchecked(AsInt() <= other.AsInt()));
        }

        throw BinaryException.Binary(this, other, "<=");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value GreaterEq(Value other)
    {
        if (IsNumber() && other.IsNumber())
        {
            if (IsKind(ValueKind.Float) || other.IsKind(ValueKind.Float))
                return new Value(AsNumber() >= other.AsNumber());

            return new Value(unchecked(AsInt() >= other.AsInt()));
        }

        throw BinaryException.Binary(this, other, ">=");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Negate()
    {
        if (IsKind(ValueKind.Int))
            return new Value(unchecked(-AsInt()));

        else if (IsKind(ValueKind.Float))
            return new Value(unchecked(-AsFloat()));

        throw UnaryException.Unary(this, "-");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Flip() => new Value(!IsTruthy());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Call(ReadOnlySpan<Value> args)
    {
        if (IsKind(ValueKind.Function))
        {
            Function function = AsFunction();

            if (args.Length != function.Arity)
                throw new ArgumentCountException($"{function} expects {function.Arity} argument(s), got {args.Length} argument(s)");

            if (function.Target != null)
                return function.Delegate(args, function.Target);

            return function.Delegate(args, null);
        }

        if (Object is ILueolCallable lueolCallable)
            return lueolCallable.LueolCall(args);

        throw new InvalidKindException($"{GetNameInQuotes} is not callable");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value GetIndex(Value index)
    {
        if (IsKind(ValueKind.String))
        {
            int raw = (int)AsInt();
            return new Value(AsString()[raw].ToString());
        }

        if (Object is ILueolGetIndex lueolGetIndex)
            if (lueolGetIndex.LueolGetIndex(index, out Value value))
                return value;

        throw new InvalidKindException($"{GetNameInQuotes()} failed to be index accessed: {ToStringWithQuotes()}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value SetIndex(Value index, Value value)
    {
        if (Object is ILueolSetIndex lueolSetIndex)
        {
            lueolSetIndex.LueolSetIndex(index, value);
            return this;
        }

        throw new InvalidKindException($"{GetNameInQuotes()} failed to be index accessed: {ToStringWithQuotes()}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value GetMember(string name)
    {
        switch (Kind)
        {
            case ValueKind.String:
                {
                    if (Builtins.String.GetStringMember(this, name, out Value value))
                        return value;
                    break;
                }
        }
        if (Object is ILueolGetMember lueolGetMember)
            if (lueolGetMember.LueolGetMember(name, out Value value))
                return value;

        throw new InvalidKindException($"{GetNameInQuotes()} does not contain member '{name}'");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value SetMember(string name, Value value)
    {
        if (Object is ILueolSetMember lueolSetMember)
        {
            lueolSetMember.LueolSetMember(name, value);
            return this;
        }

        throw new InvalidKindException($"{GetNameInQuotes()} does not contain member '{name}'");
    }

    public override int GetHashCode()
    {
        switch (Kind)
        {
            case ValueKind.Int:
                return AsInt().GetHashCode();

            case ValueKind.Float:
                return AsFloat().GetHashCode();

            case ValueKind.String:
                return AsString().GetHashCode();

            case ValueKind.Bool:
                return AsBool().GetHashCode();

            case ValueKind.Nil:
                return 0;

            case ValueKind.Function:
                return AsFunction().GetHashCode();
        }

        if (Object is ILueolHash lueolHash)
            return lueolHash.LueolGetHash();

        else if (Object is ILueolDefaultHash lueolDefaultHash)
            return lueolDefaultHash.GetHashCode();

        throw new InvalidKindException($"{GetNameInQuotes()} cannot be hashed");
    }
}