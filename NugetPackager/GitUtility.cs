using System;
using System.IO;

namespace NugetPackager
{
    public static class GitUtility
    {
        public static bool IsGitRepo(string folder)
        {
            return Directory.Exists(Path.Combine(folder, ".git"));
        }

        public static bool HasUncommittedChanges(string repoFolder)
        {
            string status = ExecuteGitCommand(repoFolder, "status");
            return status.Contains("nothing to commit") == false;
        }

        public static void Revert(string repoFolder)
        {
            ExecuteGitCommand(repoFolder, "checkout .");
        }

        private static string ExecuteGitCommand(string repoFolder, string arguments)
        {
            CommandLineResult result = CommandLine.Run(repoFolder, "git", arguments, 1, TimeUnit.Minute);
            return result.StandardOutput;
        }
    }
}
