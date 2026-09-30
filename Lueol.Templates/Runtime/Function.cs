delegate Value FunctionDelegate(ReadOnlySpan<Value> args, Value? target);

record Function
{
    public Function(string name, bool isAnonymous, string[] parameters, FunctionDelegate @delegate, Value? target)
    {
        Name = name;
        IsAnonymous = isAnonymous;
        Parameters = parameters;
        Delegate = @delegate;
        Target = target;
    }

    public string Name { get; }
    public bool IsAnonymous { get; }
    public string[] Parameters { get; }
    public FunctionDelegate Delegate { get; }
    public Value? Target { get; }

    public int Arity => Parameters.Length;

    public override string ToString()
    {
        if (IsAnonymous)
            return $"<def({string.Join(", ", Parameters)})>";
        return $"<def {Name}({string.Join(", ", Parameters)})>";
    }

    public Function Bind(Value target) =>
        new Function(Name, IsAnonymous, Parameters, Delegate, target);

    public static Function Anonymous(string[] parameters, FunctionDelegate @delegate)
        => new Function("", true, parameters, @delegate, null);

    public static Function Normal(string name, string[] parameters, FunctionDelegate @delegate)
        => new Function(name, false, parameters, @delegate, null);

    public static Function Bound(string name, string[] parameters, Value target, FunctionDelegate @delegate)
    => new Function(name, false, parameters, @delegate, target);
}