using System.Runtime.CompilerServices;

namespace Builtins
{
    static class Fs
    {
        [ModuleInitializer]
        public static void Init()
        {
            var fs = Globals.AddNamespace("fs");

            fs.AddFunction(Function.Normal("read_all_text", ["path"], (args, target) =>
            {
                string text = File.ReadAllText(args[0].ToString());
                return new Value(text);
            }));

            fs.AddFunction(Function.Normal("read_all_lines", ["path"], (args, target) =>
            {
                string[] lines = File.ReadAllLines(args[0].ToString());
                var vector = new VectorObject(lines.Length);
                foreach (string line in lines)
                    vector.Add(new Value(line));
                return new Value(vector);
            }));


            fs.AddFunction(Function.Normal("write_all_text", ["path", "text"], (args, target) =>
            {
                string path = args[0].ToString();
                string text = args[1].ToString();
                File.WriteAllText(path, text);
                return new Value(text);
            }));

            fs.AddFunction(Function.Normal("write_all_lines", ["path", "lines"], (args, target) =>
            {
                string path = args[0].ToString();
                var lines = args[1].As<VectorObject>();
                string[] stringLines = new string[lines.Count];
                for (int i = 0; i < lines.Count; i++)
                    stringLines[i] = lines[i].ToString();
                File.WriteAllLines(path, stringLines);
                return args[1];
            }));

            fs.AddFunction(Function.Normal("append_all_text", ["path", "text"], (args, target) =>
            {
                string path = args[0].ToString();
                string text = args[1].ToString();
                File.AppendAllText(path, text);
                return new Value(text);
            }));

            fs.AddFunction(Function.Normal("append_all_lines", ["path", "lines"], (args, target) =>
            {
                string path = args[0].ToString();
                var lines = args[1].As<VectorObject>();
                string[] stringLines = new string[lines.Count];
                for (int i = 0; i < lines.Count; i++)
                    stringLines[i] = lines[i].ToString();
                File.AppendAllLines(path, stringLines);
                return args[1];
            }));

            fs.AddFunction(Function.Normal("exist", ["path"], (args, target) =>
            {
                string path = args[0].ToString();
                return new Value(File.Exists(path));
            }));

            fs.AddFunction(Function.Normal("delete", ["path"], (args, target) =>
            {
                string path = args[0].ToString();
                File.Delete(path);
                return new Value(path);
            }));

            fs.AddFunction(Function.Normal("replace", ["source", "dest"], (args, target) =>
            {
                string source = args[0].ToString();
                string dest = args[1].ToString();
                File.Replace(source, dest, null);
                return new Value(dest);
            }));

            fs.AddFunction(Function.Normal("replace_with_backup", ["source", "dest", "backup"], (args, target) =>
            {
                string source = args[0].ToString();
                string dest = args[1].ToString();
                string backup = args[2].ToString();
                File.Replace(source, dest, backup);
                return new Value(dest);
            }));

            fs.AddFunction(Function.Normal("move", ["source", "dest", "overwrite"], (args, target) =>
            {
                string source = args[0].ToString();
                string dest = args[1].ToString();
                bool overwrite = args[2].ExpectBool();
                File.Move(source, dest, overwrite);
                return new Value(dest);
            }));

            fs.AddFunction(Function.Normal("copy", ["source", "dest"], (args, target) =>
            {
                string source = args[0].ToString();
                string dest = args[1].ToString();
                bool overwrite = args[2].ExpectBool();
                File.Copy(source, dest, overwrite);
                return new Value(dest);
            }));
        }
    }
}