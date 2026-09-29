using System.Globalization;
using System.Runtime.CompilerServices;

enum ValueKind : byte
{
    Int,
    Float,
    String,
    Bool,
    Nil
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
    public Value(ValueKind kind)
    {
        Kind = kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value(object value, ValueKind kind)
    {
        Kind = kind;
        Object = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value Nil() => new Value(ValueKind.Nil);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long AsInt() => unchecked((long)Payload);

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
    public T As<T>()
    {
        if (Object is not T obj)
            throw new InvalidKindObjectException("Invalid kind object");
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKind(ValueKind kind) => Kind == kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value ExpectKind(ValueKind kind)
    {
        if (Kind != kind)
            throw new InvalidKindException($"Expected {GetNameInQuotes()}, got '{GetName()}'");

        return this;
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
    public string GetName() => ValueKindNames.GetName(Kind);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetNameInQuotes() => $"'{ValueKindNames.GetName(Kind)}'";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTruthy()
    {
        return Kind switch
        {
            ValueKind.Bool => Payload != 0,
            ValueKind.Nil => false,
            _ => true
        };
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
            _ => false
        };
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
            _ => throw new InvalidKindException($"{GetNameInQuotes()} cannot be converted into a string")
        };
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
}