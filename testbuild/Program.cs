
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace testbuild
{
    internal class Program
    {
        static void Main(string[] args)
        {
           // C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe somefile.cs
            string code = "using System; namespace EXO{class Program{static void Main(string[] args){Console.Out.WriteLine(\"Meow\");}}}";
            string code2 =
                "using System;\r\n\r\nnamespace EXO1\r\n{\r\n    class Program\r\n    {\r\n        static void Main(string[] args)\r\n        {\r\n            int x = 15;\r\n            for (int i = 0; i < x; i++)\r\n            {\r\n                Console.WriteLine(i);\r\n            }\r\n        }\r\n    }\r\n}";
            builder _builder = new builder();
            _builder.exec(code2);
        }
    }
}
