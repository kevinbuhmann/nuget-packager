using System;
using System.IO;
using Vstack.Extensions;

namespace NugetPackager
{
    public static class Program
    {
        public const string ApplicationName = "Nuget Packager";

        public static void Main(string[] args)
        {
            args.ValidateNotNullParameter(nameof(args));

            Console.WindowWidth = 150;

            string oldTitle = Console.Title;
            Console.Title = ApplicationName;

            DateTime start = DateTime.Now;

            bool success = false;
            string hubSolutionFilePath = args[0];
            string version = args[1];

            if (hubSolutionFilePath.EndsWith(".sln") && File.Exists(hubSolutionFilePath))
            {
                try
                {
                    Packager.PackageProjects(hubSolutionFilePath, version);
                    success = true;
                }
                catch (CommandLineException e)
                {
                    string message = e.Result.ExitCode.HasValue ?
                        $"Error: The command `{e.Result.Command} {e.Result.Arguments}` exited with code {e.Result.ExitCode}" :
                        $"Error: The command `{e.Result.Command} {e.Result.Arguments}` timed out.";

                    Console.WriteLine();
                    Console.WriteLine(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Error: Either {hubSolutionFilePath} does not exit or it is not an solution file.");
            }

            DateTime end = DateTime.Now;
            TimeSpan runTime = end - start;

            if (success)
            {
                Console.WriteLine();
                Console.WriteLine($"Finished in {runTime.TotalSeconds} seconds");
            }

            Console.Write("Press any key to continue...");
            Console.ReadKey();

            Console.Title = oldTitle;
        }
    }
}
