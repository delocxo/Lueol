using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Clock
    {
        static Stopwatch _stopwatch = Stopwatch.StartNew();

        [ModuleInitializer]
        public static void Initialize()
        {
            Globals.AddFunction(Function.Normal("clock_start_new", [], (args, pos) =>
            {
                return new Value(new ClockObject(Stopwatch.StartNew()));
            }));

            Globals.AddFunction(Function.Normal("clock_get_timestamp", [], (args, pos) =>
            {
                return new Value(Stopwatch.GetTimestamp());
            }));

            Globals.AddFunction(Function.Normal("clock_elapsed_ms", [], (args, pos) =>
            {
                return new Value(_stopwatch.ElapsedMilliseconds);
            }));

            Globals.AddFunction(Function.Normal("clock_elapsed_ticks", [], (args, pos) =>
            {
                return new Value(_stopwatch.ElapsedTicks);
            }));
        }
    }

    record ClockObject(Stopwatch Stopwatch) : ILueolDefaultEquality, ILueolDefaultToString, ILueolGetMember, ILueolName
    {
        public string Name => "Clock";

        public bool GetMember(string name, out Value value)
        {
            switch (name)
            {
                case "elapsed_ms":
                    value = new Value(Stopwatch.ElapsedMilliseconds);
                    return true;

                case "elapsed_ticks":
                    value = new Value(Stopwatch.ElapsedTicks);
                    return true;

                case "is_running":
                    value = new Value(Stopwatch.IsRunning);
                    return true;
            }

            if (!_memberFunctions.TryGetValue(name, out var function))
            {
                value = Value.Nil();
                return false;
            }

            value = new Value(function.Bind(new Value(this)));
            return true;
        }

        static Dictionary<string, Function> _memberFunctions = new Dictionary<string, Function>
        {
            {
                "reset",
                Function.Normal("reset", [], (args, target) =>
                {
                    target!.Value.As<ClockObject>().Stopwatch.Reset();
                    return Value.Nil();
                })
            },
            {
                "restart",
                Function.Normal("restart", [], (args, target) =>
                {
                    target!.Value.As<ClockObject>().Stopwatch.Restart();
                    return Value.Nil();
                })
            },
            {
                "start",
                Function.Normal("start", [], (args, target) =>
                {
                    target!.Value.As<ClockObject>().Stopwatch.Start();
                    return Value.Nil();
                })
            },
            {
                "stop",
                Function.Normal("stop", [], (args, target) =>
                {
                    target!.Value.As<ClockObject>().Stopwatch.Stop();
                    return Value.Nil();
                })
            },
        };
    };
}