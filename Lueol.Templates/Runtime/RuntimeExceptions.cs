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

class ArgumentCountException : Exception
{
    public ArgumentCountException()
    {
    }

    public ArgumentCountException(string? message) : base(message)
    {
    }
}

class DuplicateNameException : Exception
{
    public DuplicateNameException()
    {
    }

    public DuplicateNameException(string? message) : base(message)
    {
    }
}

class UnknownMemberException : Exception
{
    public UnknownMemberException()
    {
    }

    public UnknownMemberException(string? message) : base(message)
    {
    }
}

class UserException : Exception
{
    public Value Value { get; } = Value.Nil();

    public UserException()
    {
    }

    public UserException(string? message) : base(message)
    {
    }

    public UserException(string? message, Value value) : base(message)
    {
        Value = value;
    }
}

class AssertException : Exception
{
    public AssertException()
    {
    }

    public AssertException(string? message) : base(message)
    {
    }
}