using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Enum
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            Globals.AddFunction(Function.Normal("enum", ["name", "enums"], (args, pos) =>
            {
                string name = args[0].ExpectKind(ValueKind.String).AsString();
                var vector = args[1].As<VectorObject>();
                var enums = new Dictionary<string, EnumValueObject>(vector.Values.Capacity);

                EnumObject enumObject = new EnumObject(name, enums);

                for (int i = 0; i < vector.Values.Count; i++)
                {
                    string value = vector.Values[i]
                        .ExpectKind(ValueKind.String)
                        .AsString();

                    if (!enums.TryAdd(value, new(enumObject, value, i)))
                        throw new DuplicateNameException($"'{value}' Is a duplicate name of {enumObject.ToLueolToString()}");
                }

                return new Value(enumObject);
            }));
        }
    }

    record EnumObject(string EnumName, Dictionary<string, EnumValueObject> Enums) :
        ILueolGetMember, ILueolToString, ILueolDefaultEquality, ILueolName, ILueolGetIndex
    {
        public string Name => "enum";

        public bool GetMember(string name, out Value value)
        {
            if (!Enums.TryGetValue(name, out var enumValue))
            {
                value = Value.Nil();
                return false;
            }

            value = new Value(enumValue);
            return true;
        }

        public string ToLueolToString()
            => $"<enum {EnumName}>";

        public bool GetIndex(Value index, out Value value)
        {
            string name = index.ExpectKind(ValueKind.String).AsString();
            if (!Enums.TryGetValue(name, out var enumValue))
            {
                value = Value.Nil();
                return false;
            }
            value = new Value(enumValue);
            return true;
        }
    }

    record EnumValueObject(EnumObject EnumObject, string ValueName, int Index) : ILueolGetMember, ILueolToString, ILueolEquality, ILueolName
    {
        public string Name => "enum_value";

        public bool GetMember(string name, out Value value)
        {
            switch (name)
            {
                case "enum_name":
                    value = new Value(EnumObject.EnumName);
                    return true;

                case "enum":
                    value = new Value(EnumObject);
                    return true;

                case "name":
                    value = new Value(ValueName);
                    return true;

                case "index":
                    value = new Value(Index);
                    return true;
            }

            value = Value.Nil();
            return false;
        }

        public string ToLueolToString()
            => $"{EnumObject.EnumName}.{ValueName}";

        public bool Equality(Value other)
        {
            if (!other.TryAs<EnumValueObject>(out var otherEnumValue))
                return false;

            return EnumObject == otherEnumValue!.EnumObject &&
                EnumObject.Name == otherEnumValue.EnumObject.Name &&
                ValueName == otherEnumValue.ValueName;
        }
    }
}