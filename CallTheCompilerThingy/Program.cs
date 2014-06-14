using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompilerThingy;
using System.Reflection;

namespace CallTheCompilerThingy
{
    class Program
    {
        static void Main(string[] args)
        {
            var fsc = new FSharpCompilerService();
            var references = new List<string>
            {
                @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll",
                @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll",
                @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll"
            };

            var sources = new List<string>
            {
                @"C:\Projects\FSharp.Compiler.Service\CompilerThingy\File1.fs"
            };

            var asmData = fsc.Compile(sources, "SomeTestOutputAssemblyName", references);

            var assembly = Assembly.Load(asmData);

            var t = assembly.GetTypes().Single(x => x.Name == "SomeType");
            dynamic something = Activator.CreateInstance(t);
            string s = something.SomeMember;

        }
    }
}
