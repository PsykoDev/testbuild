using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace testbuild
{
    internal class Program
    {
        private static int _lineCount;
        private static readonly StringBuilder _output = new StringBuilder();

        static void Main(string[] args)
        {
            string code = "using System; namespace EXO{class Program{static void Main(string[] args){Console.WriteLine(args[0]);}}}";
            exec(code);
        }

        private static void exec(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code, new(LanguageVersion.CSharp8));
            string? basePath = Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location);
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;
            if (root != null)
            {
                var references = root.Usings;
                var referencePaths = new List<string> {
                    typeof(object).GetTypeInfo().Assembly.Location,
                    typeof(Console).GetTypeInfo().Assembly.Location,
                    Path.Combine(basePath, "System.Runtime.dll"),
                    Path.Combine(basePath, "System.Runtime.Extensions.dll"),
                    Path.Combine(basePath, "mscorlib.dll")
                };
                referencePaths.AddRange(references.Select(x => Path.Combine(basePath, $"{x.Name}.dll")));
                var executableReferences = new List<PortableExecutableReference>();
                foreach (var reference in referencePaths)
                {
                    if (File.Exists(reference))
                    {
                        executableReferences.Add(MetadataReference.CreateFromFile(reference));
                    }
                }



                var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), new[] { syntaxTree }, executableReferences, new(OutputKind.WindowsApplication));
                

                using (var memoryStream = new MemoryStream())
                {
                    EmitResult compilationResult = compilation.Emit(memoryStream);
                    if (!compilationResult.Success)
                    {
                        var errors = compilationResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)?.ToList() ?? new List<Diagnostic>();
                        StringBuilder sb = new StringBuilder();
                        foreach (var error in errors)
                        {
                            sb.AppendLine($"{error.Id}: {error.GetMessage()}");
                        }
                        Console.WriteLine(sb.ToString());
                    }
                    else
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        AssemblyLoadContext assemblyContext = new AssemblyLoadContext(Path.GetRandomFileName(), true);
                        Assembly assembly = assemblyContext.LoadFromStream(memoryStream);
                        var entryPoint = compilation.GetEntryPoint(CancellationToken.None);
                        var type = assembly.GetType($"{entryPoint.ContainingNamespace.MetadataName}.{entryPoint.ContainingType.MetadataName}");
                        var instance = assembly.CreateInstance(type.FullName);
                        var method = type.GetMethod(entryPoint.MetadataName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Default);
                        object oResult = method.Invoke(instance, BindingFlags.Default | BindingFlags.InvokeMethod, Type.DefaultBinder, new object[] { new string[] { "Meiow" } }, null);
                        assemblyContext.Unload();
                        //Console.WriteLine(Exec("cmd", method.Module.ScopeName));
                        if (oResult != null)
                        {
                            Console.WriteLine("ozinfrnoiu" +oResult.ToString());
                        }
                    }


                }
            }

        }

        private static string Exec(string filename, string cmd)
        {
            string result;
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = filename;
            startInfo.Arguments = "/C " + cmd;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) =>
            {

                if (!string.IsNullOrEmpty(e.Data))
                {
                    _lineCount++;
                    if (_lineCount == 1)
                    {
                        _output.Append(e.Data);
                    }
                    else if (_lineCount == 2)
                    {
                        _output.Append("\n" + e.Data + "\n");
                    }
                    else
                    {
                        _output.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _lineCount++;
                    _output.AppendLine("[" + _lineCount + "]: " + e.Data);
                }
            };

            process.StartInfo = startInfo;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            process.WaitForExit();
            process.Close();
            result = _output.ToString();
            _lineCount = 0;
            _output.Clear();
            return result;
        }
    }
}
