class InvalidKindException : Exception
{
    public InvalidKindException()
    {
    }

    public InvalidKindException(string? message) : base(message)
    {
    }
}

class InvalidKindObjectException : Exception
{
    public InvalidKindObjectException()
    {
    }

    public InvalidKindObjectException(string? message) : base(message)
    {
    }
}

class BinaryException : Exception
{
    public BinaryException()
    {
    }

    public BinaryException(string? message) : base(message)
    {
    }

    public static BinaryException Binary(Value left, Value right, string op)
        => new BinaryException($"Cannot apply '{op}' to {left.GetNameInQuotes()} and {right.GetNameInQuotes()}");
}

class UnaryException : Exception
{
    public UnaryException()
    {
    }

    public UnaryException(string? message) : base(message)
    {

    }

    public static UnaryException Unary(Value right, string op)
        => new UnaryException($"Cannot apply '{op}' to {right.GetNameInQuotes()}");
}