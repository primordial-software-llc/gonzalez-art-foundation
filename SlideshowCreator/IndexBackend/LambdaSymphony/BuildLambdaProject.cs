using System;
using System.Diagnostics;
using Amazon.Lambda;

namespace IndexBackend.LambdaSymphony
{
    class BuildLambdaProject
    {
        private static void Clean(string projectPath)
        {
            var proc = Process.Start("dotnet", $"clean {projectPath}");
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Clean failed with exit code: " + proc.ExitCode);
            }
        }

        private static void BuildCore(string projectPath, string outputPath)
        {
            var finalOutput = $@"{outputPath}\Release";
            var buildArguments = $@"build {projectPath} -o {finalOutput} -c Release";
            var proc = Process.Start("dotnet", buildArguments);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Build failed with exit code: " + proc.ExitCode);
            }
        }

        public static string Build(string projectPath, string outputPath, Runtime runtime)
        {
            BuildCore(projectPath, outputPath);

            Clean(projectPath); // Starting the test process clears needed project files.

            Console.WriteLine($"restore {projectPath}");
            var proc = Process.Start("dotnet", $"restore {projectPath}");
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Restore failed with exit code: " + proc.ExitCode);
            }

            BuildCore(projectPath, outputPath);

            var wd = projectPath.Split('\\');
            var projectFileName = wd[wd.Length - 1];
            wd[wd.Length - 1] = string.Empty;
            string workingDirectory = string.Join(@"\", wd);

            string framework;
            if (runtime == Runtime.Dotnetcore10)
            {
                framework = "netcoreapp1.0";
            }
            else if (runtime == Runtime.Dotnetcore20)
            {
                framework = "netcoreapp2.0";
            }
            else if (runtime == Runtime.Dotnetcore21)
            {
                framework = "netcoreapp2.1";
            }
            else if (runtime == Runtime.Dotnetcore31)
            {
                framework = "netcoreapp3.1";
            }
            else
            {
                throw new Exception("Unknown runtime " + runtime.Value);
            }

            proc = Process.Start(new ProcessStartInfo("dotnet", $@"lambda package --configuration release --framework {framework}")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                ErrorDialog = true
            });

            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Build failed with exit code: " + proc.ExitCode);
            }

            var pn = projectFileName.Split('.');
            pn[pn.Length - 1] = "zip";
            var projectName = string.Join(".", pn);
            return workingDirectory + $"\\bin\\Release\\{framework}\\" + projectName;
        }
    }
}
