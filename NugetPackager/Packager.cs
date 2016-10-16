﻿using Microsoft.CodeAnalysis;
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
        public static void PackageProjects(string hubSolutionFilePath, string version)
        {
            IEnumerable<string> projectFilePaths = GetProjects(hubSolutionFilePath)
                .Select(project => project.FilePath)
                .ToList();

            IEnumerable<string> solutionFilePaths = GetSolutionFilePaths(projectFilePaths);

            CheckForUncommittedChanges(solutionFilePaths);
            NugetRestore(solutionFilePaths);
            NugetPack(solutionFilePaths, version);

            // commit AssemblyInfo.cs changes
            RevertChanges(solutionFilePaths);
        }

        private static IEnumerable<Project> GetProjects(string solutionFilePath)
        {
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
                throw new Exception($"Unable to find solution path for {projectFilePath}");
            }

            return projectSolutionFilePath;
        }

        private static void CheckForUncommittedChanges(IEnumerable<string> solutionFilePaths)
        {
            foreach (string solutionFilePath in solutionFilePaths)
            {
                string solutionPath = Path.GetDirectoryName(solutionFilePath);

                if (GitUtility.IsGitRepo(solutionPath) && GitUtility.HasUncommittedChanges(solutionPath))
                {
                    throw new Exception($"{solutionPath} has uncommitted changes.");
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
            foreach (string solutionFilePath in solutionFilePaths)
            {
                string solutionName = Path.GetFileNameWithoutExtension(solutionFilePath);
                Project mainProject = GetProjects(solutionFilePath)
                    .FirstOrDefault(project => project.Name == solutionName);

                if (mainProject != null)
                {
                    if (mainProject.ProjectReferences.Any())
                    {
                        Console.WriteLine($"{mainProject.Name} has unresolved local project references.");
                    }
                    else
                    {
                        UpdateAssemblyVersion(mainProject, version);
                        MSBuild(solutionFilePath);
                        NugetPack(mainProject.FilePath);
                    }

                    Console.WriteLine();
                }
                else
                {
                    throw new Exception($"Unable to find main project in solution {solutionName}.");
                }
            }
        }

        private static void UpdateAssemblyVersion(Project project, string version)
        {
            Document assemblyInfoDocument = project.Documents
                .FirstOrDefault(document => document.Name == "AssemblyInfo.cs");

            if (assemblyInfoDocument != null)
            {
                string code = File.ReadAllText(assemblyInfoDocument.FilePath);
                code = Regex.Replace(code, "\\[assembly:\\s+AssemblyVersion\\(\".+\"\\)\\]", $"[assembly: AssemblyVersion(\"{version}\")]");
                File.WriteAllText(assemblyInfoDocument.FilePath, code, Encoding.UTF8);

                Console.WriteLine($"Updated {project.Name} version to {version}.");
            }
            else
            {
                throw new Exception($"Unable to update {project.Name} version.");
            }
        }

        private static void MSBuild(string solutionFilePath)
        {
            string solutionPath = Path.GetDirectoryName(solutionFilePath);
            string solutionFileName = Path.GetFileName(solutionFilePath);

            CommandLine.Run(solutionPath, "msbuild", $"{solutionFileName} /p:Configuration=Release", 5, TimeUnit.Minute);
        }

        private static void NugetPack(string projectFilePath)
        {
            string projectPath = Path.GetDirectoryName(projectFilePath);
            string projectFileName = Path.GetFileName(projectFilePath);

            CommandLine.Run(projectPath, "nuget", $"pack {projectFileName}", 5, TimeUnit.Minute);
        }

        private static void RevertChanges(IEnumerable<string> solutionFilePaths)
        {
            foreach (string solutionFilePath in solutionFilePaths)
            {
                string solutionPath = Path.GetDirectoryName(solutionFilePath);

                if (GitUtility.IsGitRepo(solutionPath))
                {
                    GitUtility.Revert(solutionPath);
                }
            }

            Console.WriteLine();
        }

        ////private static bool HasNuspec(string projectPath)
        ////{
        ////    string projectFileName = Path.GetFileName(projectPath);
        ////    string nuspecFilePath = Path.Combine(projectPath, $"{projectFileName}.nuspec");
        ////    return File.Exists(nuspecFilePath);
        ////}
    }
}
