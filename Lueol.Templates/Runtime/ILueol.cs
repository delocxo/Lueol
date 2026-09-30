interface ILueolGetIndex
{
    public bool GetIndex(Value index, out Value value);
}

interface ILueolSetIndex
{
    public void SetIndex(Value index, Value value);
}

interface ILueolGetMember
{
    public bool GetMember(string name, out Value value);
}

interface ILueolSetMember
{
    public void SetMember(string name, Value value);
}

interface ILueolEquality
{
    public bool Equality(Value other);
}

interface ILueolDefaultEquality
{
    public bool DefaultEquality(Value other)
    {
        if (other.Kind != ValueKind.Object)
            return false;

        return Equals(this, other.Object);
    }
}

interface ILueolToString
{
    public string ToLueolToString();
}

interface ILueolIsTruthy
{
    public bool IsTruthy();
}