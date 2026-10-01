using System.Globalization;

namespace Builtins
{
    static class String
    {
        static Dictionary<string, Function> _stringFunctions = new()
        {
            {
                "trim",
                Function.Normal("trim", [], (args, target) =>
                {
                    return new Value(target!.Value.AsString().Trim());
                })
            },
            {
                "trim_start",
                Function.Normal("trim_start", [], (args, target) =>
                {
                    return new Value( target!.Value.AsString().TrimStart());
                })
            },
            {
                "trim_end",
                Function.Normal("trim_end", [], (args, target) =>
                {
                    return new Value( target!.Value.AsString().TrimEnd());
                })
            },
            {
                "starts_with",
                Function.Normal("starts_with", ["needle"], (args, target) =>
                {
                    return new Value( target!.Value.AsString().StartsWith(args[0].ToString()));
                })
            },
            {
                "ends_with",
                Function.Normal("ends_with", ["needle"], (args, target) =>
                {
                    return new Value( target!.Value.AsString().EndsWith(args[0].ToString()));
                })
            },
            {
                "contains",
                Function.Normal("contains", ["needle"], (args, target) =>
                {
                    return new Value( target!.Value.AsString().Contains(args[0].ToString()));
                })
            },
            {
                "index_of",
                Function.Normal("index_of", ["needle"], (args, target) =>
                {
                    return new Value( target!.Value.AsString().IndexOf(args[0].ToString()));
                })
            },
            {
                "slice",
                Function.Normal("slice", ["start", "end"], (args, target) =>
                {
                    int start = args[0].ExpectKind(ValueKind.Int).AsInt32();
                    int end = args[1].ExpectKind(ValueKind.Int).AsInt32();
                    return new Value( target!.Value.AsString().Substring(start, end - start));
                })
            },
            {
                "to_vector",
                Function.Normal("to_vector", [], (args, target) =>
                {
                    VectorObject vectorObject = new VectorObject([]);
                    foreach (char c in target!.Value.AsString())
                        vectorObject.Values.Add(new Value(c.ToString()));
                    return new Value(vectorObject);
                })
            },
        };

        public static bool GetStringMember(Value target, string name, out Value value)
        {
            string text = target.AsString();

            switch (name)
            {
                case "length":
                    value = new Value(text.Length);
                    return true;

                case "is_empty":
                    value = new Value(text.Length == 0);
                    return true;

                case "is_space":
                    value = new Value(string.IsNullOrWhiteSpace(text));
                    return true;

                case "is_int":
                    value = new Value(long.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _));
                    return true;

                case "is_number":
                    value = new Value(double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _));
                    return true;

                case "is_alpha":
                    foreach (char c in text)
                        if (!char.IsLetter(c))
                        {
                            value = new Value(false);
                            return true;
                        }
                    value = new Value(true);
                    return true;

            }

            if (_stringFunctions.TryGetValue(name, out var function))
            {
                value = new Value(function.Bind(target));
                return true;
            }

            value = Value.Nil();
            return false;
        }
    }
}