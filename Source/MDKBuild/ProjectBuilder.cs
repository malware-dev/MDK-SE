using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build.Composers;
using MDK.Build.Composers.Default;
using MDK.Build.Composers.Minifying;
using MDK.Build.Solution;
using MDK.Build.TypeTrimming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MDK.Build
{
    /// <summary>
    /// Builds a single project.
    /// </summary>
    public class ProjectBuilder
    {
        public readonly Project project;
        public readonly MDKProjectProperties config;

        public string Destination {
            get {
                return BuildModule.ExpandMacros(
                    project,
                    Path.Combine(
                        Path.GetDirectoryName(project.FilePath),
                        config.Paths.OutputPath,
                        project.Name
                    )
                );
            }
        }
        public readonly string ScriptName = "script.cs";

        public ProjectBuilder(Project project)
        {
            this.project = project;
            config = MDKProjectProperties.Load(project.FilePath, project.Name);

            // There was a check here to make sure the config's name is the same as the projects.
            // Im not sure why it was here
            // TODO: put it back

            if (!config.IsValid)
                // TODO: _why_ is this not valid? Print out a useful reason.
                throw new Exception($"Config for project \"{project.Name}\" is not valid. This is a bad error message");
        }

        public async Task<string> BuildScript(IProgress<float> progress = null)
        {
            progress?.Report(0/3f);
            
            var documentComposer = new ProgramDocumentComposer();
            var composition = await documentComposer
                .ComposeAsync(project, config);

            progress?.Report(1/3f);

            if (config.Options.TrimTypes)
            {
                var processor = new TypeTrimmer();
                composition = await processor.ProcessAsync(composition, config);
            }
            progress?.Report(2/3f);

            ScriptComposer composer;
            switch (config.Options.MinifyLevel)
            {
                case MinifyLevel.Full: composer = new MinifyingComposer(); break;
                case MinifyLevel.StripComments: composer = new StripCommentsComposer(); break;
                case MinifyLevel.Lite: composer = new LiteComposer(); break;
                default: composer = new DefaultComposer(); break;
            }
            var script = await composer
                .GenerateAsync(composition, config);
                
            progress?.Report(3/3f);

            if (composition.Readme != null)
            {
                script = composition.Readme + script;
            }
            return script;
        }
        
        public void WriteScript(string script)
        {
            // TODO: I think this is wrong somehow
            var outputInfo = new DirectoryInfo(Destination);
            if (!outputInfo.Exists)
                outputInfo.Create();
            
            File.WriteAllText(Path.Combine(Destination, ScriptName), script.Replace("\r\n", "\n"), Encoding.UTF8);

            var thumbFile = new FileInfo(Path.Combine(Path.GetDirectoryName(project.FilePath) ?? ".", "thumb.png"));
            if (thumbFile.Exists)
                thumbFile.CopyTo(Path.Combine(outputInfo.FullName, "thumb.png"), true);
        }
    }
}