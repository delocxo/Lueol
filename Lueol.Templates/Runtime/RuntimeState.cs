sealed class RuntimeContext
{
    public Position Position;
}

sealed class RuntimeState
{
    static readonly AsyncLocal<RuntimeContext?> _context = new();

    public static RuntimeContext Context
    {
        get => _context.Value ??= new RuntimeContext();
    }

    public static Position Position
    {
        get => Context.Position;
        set => Context.Position = value;
    }
    public static void Begin()
    {
        _context.Value = new RuntimeContext();
    }
}