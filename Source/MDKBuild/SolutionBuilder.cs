using JetBrains.Annotations;
using Malware.MDKServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSSolution = Microsoft.CodeAnalysis.Solution;

namespace MDK.Build
{
    public class SolutionBuilder
    {
        public readonly VSSolution Solution;
        public readonly Dictionary<ProjectId, ProjectBuilder> ProjectBuilders;

        public SolutionBuilder(VSSolution solution)
        {
            Solution = solution;
            ProjectBuilders = solution.Projects
                .Select(project => new ProjectBuilder(project))
                .Where(builder => builder.config.IsValid && !builder.config.Options.ExcludeFromDeployAll)
                .ToDictionary(builder => builder.project.Id);
        }

        public static async Task<MDKProjectProperties[]> BuildByName(
            string solutionPath,
            string targetProjectPath = null,
            IProgress<float> progress = null
            )
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(solutionPath);
            var builder = new SolutionBuilder(solution);
            if (targetProjectPath != null)
            {
                var targetBuilder = builder.ProjectBuilders.Values
                    .First(projectBuilder => projectBuilder.project.FilePath == targetProjectPath);
                await builder.BuildProject(targetBuilder.project.Id, progress);
                return new[] { targetBuilder.config }; 
            } else
            {
                await builder.BuildAll(progress);
                return builder.ProjectBuilders.Values
                    .Select(projectBuilder => projectBuilder.config)
                    .ToArray();
            }
        }


        public async Task BuildAll(IProgress<float> progress = null)
        {
            var totalProgress = 0f;

            await Task.WhenAll(ProjectBuilders.Values.Select(async (builder) =>
            {
                var buildProgress = new Progress<float>();
                buildProgress.ProgressChanged += (s, e) =>
                {
                    totalProgress += e / ProjectBuilders.Count;
                    progress?.Report(totalProgress);
                };
                var script = await builder.BuildScript(buildProgress);
                builder.WriteScript(script);
            }));
        }

        public async Task BuildProject(ProjectId projectId, IProgress<float> progress = null)
        {
            var success = ProjectBuilders.TryGetValue(projectId, out var builder);
            if (!success)
                throw new Exception($"The project \"{projectId}\" was not found inside the solution \"{Solution.Id}\"");
            var script = await builder.BuildScript(progress);
            builder.WriteScript(script);
        }
    }
}
