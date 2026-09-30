static class Globals
{
    static Dictionary<string, Value> s_globals { get; } = [];

    public static void AddGlobal(string name, Value value)
        => s_globals[name] = value;

    public static Value GetGlobal(string name)
    {
        if (!s_globals.TryGetValue(name, out Value value))
            throw new InvalidOperationException($"'{name}' does not exist");
        return value;
    }

    public static void AddFunction(Function function)
    {
        if (string.IsNullOrWhiteSpace(function.Name) || function.IsAnonymous)
            throw new InvalidOperationException("Function cannot be anonymous or have an empty name");
        s_globals[function.Name] = new Value(function);
    }
}