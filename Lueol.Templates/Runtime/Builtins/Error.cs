using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Error
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            Globals.AddFunction(Function.Normal("assert", ["condition", "message"], (args, pos) =>
            {
                if (!args[0].IsTruthy())
                    throw new AssertException(args[1].ToString());
                return Value.Nil();
            }));

            Globals.AddFunction(Function.Normal("error", ["message"], (args, pos) =>
            {
                throw new UserException(args[0].ToString());
            }));

            Globals.AddFunction(Function.Normal("error_value", ["message", "value"], (args, pos) =>
            {
                throw new UserException(args[0].ToString(), args[1]);
            }));

            Globals.AddFunction(Function.Normal("attempt", ["callback"], (args, pos) =>
            {
                try
                {
                    Value data = args[0].Call([]);
                    return MakeAttemptResult(true, true, data, "", "");
                }
                catch (UserException u)
                {
                    return MakeAttemptResult(false, true, u.Value, u.Message, u.GetType().Name);
                }
                catch (Exception e)
                {
                    return MakeAttemptResult(false, false, Value.Nil(), e.Message, e.GetType().Name);
                }
            }));
        }

        static Value MakeAttemptResult(bool success, bool hasData, Value data, string error, string errorName)
        {
            ShapeObject shapeObject = ShapeObject.Create(
                [
                    ("success", new Value(success)),
                    ("has_data", new Value(hasData)),
                    ("data", data),
                    ("error", new Value(error)),
                    ("error_name", new Value(errorName))
                ]
            );
            return new Value(shapeObject);
        }
    }
}