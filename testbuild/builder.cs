using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace testbuild
{
    public class builder
    {
        private int _lineCount;
        private readonly StringBuilder _output = new StringBuilder();
        public void exec(string code)
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
                    Path.Combine(basePath, "System.dll"),
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
                        Console.Error.WriteLine(sb.ToString());
                    }
                    else
                    {
                        using (var outputCapture = new OutputCapture())
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            AssemblyLoadContext assemblyContext = new AssemblyLoadContext(Path.GetRandomFileName(), true);
                            Assembly assembly = assemblyContext.LoadFromStream(memoryStream);
    
                            var entryPoint = compilation.GetEntryPoint(CancellationToken.None);
                            var type = assembly.GetType($"{entryPoint.ContainingNamespace.MetadataName}.{entryPoint.ContainingType.MetadataName}");
    
                            var instance = assembly.CreateInstance(type.FullName);
                            var method = type.GetMethod(entryPoint.MetadataName,
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.Default);

                            method.Invoke(instance, BindingFlags.Instance, null, new object[] { new string[] { "" } }, null);

                            _output.Append(outputCapture.Captured.ToString());
                            Console.WriteLine("Captured Data "+_output.ToString());
                            
                            assemblyContext.Unload();
                        }
                    }
                }
            }
        }
    }
}
