using System.Text;
using Microsoft.CodeAnalysis.CSharp;

class SourceGenerator
{
    readonly string _exePath = AppContext.BaseDirectory;
    string _user;

    public SourceGenerator(StringBuilder user)
    {
        _user = user.ToString();
    }

    public string Generate()
    {
        StringBuilder result = new StringBuilder();

        string runtime = GetRuntime();

        var stripped = StripUsingsAndPackages(runtime);

        foreach (string package in stripped.Packages)
            result.AppendLine(package);

        result.AppendLine();

        foreach (string use in stripped.Usings)
            result.AppendLine(use);

        result.AppendLine();

        result.AppendLine("Value LueolMain()");
        result.AppendLine("{");
        result.Append(_user);
        result.AppendLine("    return Value.Nil();");
        result.AppendLine("}");

        result.AppendLine();

        result.AppendLine("try");
        result.AppendLine("{");
        result.AppendLine("    LueolMain();");
        result.AppendLine("    return 0;");
        result.AppendLine("}");

        result.AppendLine("catch (Exception e)");
        result.AppendLine("{");
        result.AppendLine("    Position pos = RuntimeState.Position;");
        result.AppendLine("    Console.Error.WriteLine($\"[{e.GetType().Name}]: {pos.Line}:{pos.Column}:{pos.Source}: {e.Message}\");");
        result.AppendLine("    return 1;");
        result.AppendLine("}");



        result.AppendLine();

        result.AppendLine(stripped.Result);

        return result.ToString();
    }

    (HashSet<string> Usings, HashSet<string> Packages, string Result) StripUsingsAndPackages(string str)
    {
        HashSet<string> usings = [];
        HashSet<string> pacakges = [];
        StringBuilder result = new StringBuilder();

        using StringReader stringReader = new StringReader(str);

        string? line;

        while ((line = stringReader.ReadLine()) != null)
        {
            string trimmed = line.Trim();

            var lineRoot = CSharpSyntaxTree
                .ParseText(trimmed)
                .GetCompilationUnitRoot();

            var uses = lineRoot.Usings
                .Select(x => x.ToString())
                .ToList();

            if (uses.Count > 0)
            {
                foreach (string use in lineRoot.Usings.Select(x => x.ToString()))
                {
                    string newUse = use;
                    if (newUse.StartsWith("global "))
                        newUse = newUse["global ".Length..];
                    usings.Add(newUse);
                }
            }
            else if (trimmed.StartsWith("// #:package "))
            {
                pacakges.Add(trimmed["// ".Length..]);
            }
            else
            {
                result.AppendLine(line);
            }
        }

        return (usings, pacakges, result.ToString());
    }

    string GetRuntime()
    {
        string runtimePath = Path.Join(_exePath, "Templates/Runtime");
        if (!Directory.Exists(runtimePath))
            throw new InvalidOperationException($"Failed to locate runtime folder: '{runtimePath}'");

        StringBuilder builder = new StringBuilder();

        foreach (var file in Directory.EnumerateFiles(runtimePath, "*.cs", SearchOption.AllDirectories))
        {
            string contents = CheckFile(file);
            builder.Append(contents);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    string CheckFile(string path)
    {
        string contents = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(contents);
        var diagnostics = tree
            .GetDiagnostics()
            .Where(x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        if (diagnostics.Count > 0)
        {
            string message = string.Join(
                Environment.NewLine,
                diagnostics.Select(x => x.ToString())
            );

            throw new InvalidOperationException($$"""
            Imported file: '{{path}}' has invalid csharp 
            {{message}}
            """);
        }

        return contents;
    }
}