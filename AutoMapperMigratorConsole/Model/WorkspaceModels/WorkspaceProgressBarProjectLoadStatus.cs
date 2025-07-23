using System;
using Microsoft.CodeAnalysis.MSBuild;

namespace AutoMapperMigratorConsole.Model.WorkspaceModels
{
    public sealed class WorkspaceProgressBarProjectLoadStatus : IProgress<ProjectLoadProgress>
    {
        public void Report(ProjectLoadProgress value)
        {
            Console.Out.WriteLine($"{value.Operation} {value.FilePath}");
        }
    }
}