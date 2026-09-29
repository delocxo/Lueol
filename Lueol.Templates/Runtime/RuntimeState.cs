sealed class RuntimeState
{
    static readonly AsyncLocal<Position> _position = new();

    public static Position Position
    {
        get => _position.Value;
        set => _position.Value = value;
    }
}