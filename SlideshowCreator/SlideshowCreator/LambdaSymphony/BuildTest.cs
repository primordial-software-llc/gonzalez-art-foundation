using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;

namespace SlideshowCreator.LambdaSymphony
{
    class BuildTest
    {
        [Test]
        public void Build()
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Project project = workspace
                .OpenProjectAsync(@"C:\Users\peon\Desktop\projects\Nest\Nest\Nest.csproj")
                .Result;

            EmitResult result;
            Compilation projectCompilation = project.GetCompilationAsync().Result;

            Console.WriteLine("Building: " + projectCompilation.AssemblyName);
            foreach (MetadataReference externalReference in projectCompilation.ExternalReferences)
            {
                var output = @"C:\Users\peon\Desktop\Build\" + externalReference.Display.Split('\\').Last();
                File.Copy(externalReference.Display, output, true);
            }

            Console.WriteLine("Dependency copy complete");

            using (var stream = new MemoryStream())
            {
                result = projectCompilation.Emit(stream);
                if (result.Success)
                {
                    string fileName = projectCompilation.AssemblyName + ".dll";

                    using (FileStream file = File.Create(@"C:\Users\peon\Desktop\Build" + '\\' + fileName))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(file);
                    }
                }
            }

            Assert.IsTrue(result.Success);
        }



    }
}
