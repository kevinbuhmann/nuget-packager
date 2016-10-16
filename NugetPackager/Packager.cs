using GitCompare;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NugetPackager
{
    public static class Packager
    {
        public static void PackageProjects(string hubSolutionFilePath, string version, string branch)
        {
            IEnumerable<string> projectFilePaths = GetProjects(hubSolutionFilePath)
                .Select(proj => proj.FilePath)
                .ToList();

            IEnumerable<string> solutionFilePaths = GetSolutionFilePaths(projectFilePaths);

            IEnumerable<string> solutionPaths = GetSolutionFilePaths(projectFilePaths)
                .Select(solutionFilePath => Path.GetDirectoryName(solutionFilePath))
                .ToList();

            EnsureReposAreCleanAndUpToDate(solutionPaths, branch);
            NugetRestore(solutionFilePaths);
            NugetPack(solutionFilePaths, version);

            // commit AssemblyInfo.cs changes
        }

        private static IEnumerable<Project> GetProjects(string solutionFilePath)
        {
            Console.WriteLine($"Getting projects in {solutionFilePath}.");

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = workspace.OpenSolutionAsync(solutionFilePath).Result;

                return solution
                    .GetProjectDependencyGraph()
                    .GetTopologicallySortedProjects()
                    .Select(projectId => solution.GetProject(projectId))
                    .ToList();
            }
        }

        private static IEnumerable<string> GetSolutionFilePaths(IEnumerable<string> projectFilePaths)
        {
            return projectFilePaths
                .Select(projectFilePath => GetSolutionFilePath(projectFilePath))
                .ToList();
        }

        private static string GetSolutionFilePath(string projectFilePath)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            string projectPath = Path.GetDirectoryName(projectFilePath);
            string projectParentPath = Directory.GetParent(projectPath).FullName;
            string projectSolutionFilePath = Path.Combine(projectParentPath, $"{projectName}.sln");

            if (!File.Exists(projectSolutionFilePath))
            {
                throw new ExpectationFailedException($"Solution for project {projectName} does not exist at {projectSolutionFilePath}.");
            }

            return projectSolutionFilePath;
        }

        private static void EnsureReposAreCleanAndUpToDate(IEnumerable<string> solutionPaths, string branch)
        {
            foreach (string solutionPath in solutionPaths)
            {
                Console.WriteLine($"Checking status of {solutionPath}.");

                if (GitUtility.IsGitRepo(solutionPath))
                {
                    string currentBranch = GitUtility.GetCurrentBranch(solutionPath);
                    RepoStatus repoStatus = GitUtility.GetRepoStatus(solutionPath);

                    if (currentBranch != branch)
                    {
                        throw new ExpectationFailedException($"{solutionPath} is checked out to {currentBranch}, expected {branch}.");
                    }

                    if (repoStatus != RepoStatus.CleanAndUpToDate)
                    {
                        throw new ExpectationFailedException($"{solutionPath} is not clean and up to date: {repoStatus.ToStatusString()}.");
                    }
                }
                else
                {
                    throw new ExpectationFailedException($"{solutionPath} is not a git repo.");
                }
            }

            Console.WriteLine();
        }

        private static void NugetRestore(IEnumerable<string> solutionFilePaths)
        {
            foreach (string solutionFilePath in solutionFilePaths)
            {
                string solutionPath = Path.GetDirectoryName(solutionFilePath);
                string solutionFileName = Path.GetFileName(solutionFilePath);

                CommandLine.Run(solutionPath, "nuget", $"restore {solutionFileName}", 5, TimeUnit.Minute);
            }

            Console.WriteLine();
        }

        private static void NugetPack(IEnumerable<string> solutionFilePaths, string version)
        {
            List<string> packages = new List<string>();

            foreach (string solutionFilePath in solutionFilePaths)
            {
                string solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);
                IEnumerable<Project> projects = GetProjects(solutionFilePath);

                Project project = projects
                    .FirstOrDefault(proj => proj.Name == solutionName);

                if (project != null)
                {
                    UpdateAssemblyVersion(project, version);
                    MSBuild(solutionFilePath);
                    NugetPack(project.FilePath);

                    packages.Add(project.Name);

                    Console.WriteLine();
                }
                else
                {
                    throw new ExpectationFailedException($"Solution {solutionName} does not contain a project named {solutionName}.");
                }
            }
        }

        private static void UpdateAssemblyVersion(Project project, string version)
        {
            string asseblyInfoFileName = "AssemblyInfo.cs";

            Document assemblyInfoDocument = project.Documents
                .FirstOrDefault(doc => doc.Name == asseblyInfoFileName);

            if (assemblyInfoDocument != null)
            {
                string code = File.ReadAllText(assemblyInfoDocument.FilePath);
                code = Regex.Replace(code, "\\[assembly:\\s+AssemblyVersion\\(\".+\"\\)\\]", $"[assembly: AssemblyVersion(\"{version}\")]");
                File.WriteAllText(assemblyInfoDocument.FilePath, code, Encoding.UTF8);

                Console.WriteLine($"Updated {project.Name} version to {version}.");
            }
            else
            {
                throw new ExpectationFailedException($"Project {project.Name} does not contain a document named {asseblyInfoFileName}.");
            }
        }

        private static void MSBuild(string solutionFilePath)
        {
            string solutionPath = Path.GetDirectoryName(solutionFilePath);
            string solutionFileName = Path.GetFileName(solutionFilePath);

            CommandLine.Run(solutionPath, "msbuild", $"{solutionFileName} /t:Clean,Build /p:Configuration=Release", 5, TimeUnit.Minute);
        }

        private static void NugetPack(string projectFilePath)
        {
            string projectPath = Path.GetDirectoryName(projectFilePath);
            string projectFileName = Path.GetFileName(projectFilePath);

            CommandLine.Run(projectPath, "nuget", $"pack {projectFileName} -IncludeReferencedProjects -Prop Configuration=Release", 5, TimeUnit.Minute);
        }
    }
}
