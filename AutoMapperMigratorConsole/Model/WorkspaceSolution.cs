using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AutoMapperMigratorConsole.Model
{
    public class WorkspaceSolution : WorkspaceBase
    {
        private readonly Solution _solution;

        private readonly IDictionary<string, WorkspaceProject> _openProjects = new Dictionary<string, WorkspaceProject>();
        
        public static async Task<WorkspaceSolution> Load(string fileName)
        {
            await Console.Out.WriteLineAsync("Loading solution...");
            var workspace = NewMsBuildWorkspace();
            var solution = await workspace.OpenSolutionAsync(fileName, new ProgressBarProjectLoadStatus());
            
            return new WorkspaceSolution(solution);
        }

        public WorkspaceSolution(Solution solution)
        {
            _solution = solution;
        }

        public async Task<Dictionary<string,ISymbol>> AllProjectSymbols()
        {
            foreach (var p in _solution.Projects)
            {
                _openProjects[p.Name] = await WorkspaceProject.LoadFromSolution(this, p);
            }
            
            Dictionary<string,ISymbol> allSymbols = new Dictionary<string,ISymbol>();
            
            foreach (var p in _openProjects)
            {
                var sym = await p.Value.AllProjectSymbols();

                foreach (var item in sym)
                {
                    var key = item.ToString();
                    if (!allSymbols.TryAdd(key, item))
                    {
                        await Console.Out.WriteLineAsync($"Duplicate: {key}");
                    }
                }
            }
            
            return allSymbols;
        }

        public async Task<List<WorkspaceAutoMapper>> FindAutoMapperProfiles()
        {
            List<WorkspaceAutoMapper> profiles = new List<WorkspaceAutoMapper>();
            
            foreach (var p in _openProjects.Values)
            {
                await Console.Out.WriteLineAsync($"Scan for AutoMapperProfile {p.ProjectName}");
                
                var list = await p.FindAutoMapper();
                if (list.Any())
                {
                    await Console.Out.WriteLineAsync($"Found {list.Count} profiles in {p.DefaultNamespace}");

                    profiles.Add(new WorkspaceAutoMapper()
                    {
                        Project = p,
                        Mappings = list
                    });
                }
            }

            return profiles;
        }
        
        public async Task<WorkspaceProject> GetProject(string projectName)
        {
            if (!_openProjects.ContainsKey(projectName))
            {
                var project = _solution.Projects.SingleOrDefault(x => string.Equals(x.Name, projectName, StringComparison.OrdinalIgnoreCase));
                _openProjects[projectName] = await WorkspaceProject.LoadFromSolution(this, project);
            }

            return _openProjects[projectName];
        }
    }
}