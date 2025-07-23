using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapperMigratorConsole.WalkerCollectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Model.WorkspaceModels
{
    public sealed class WorkspaceProject : WorkspaceBase
    {
        private readonly Project _project;
        private readonly Compilation _compilation;

        public string ProjectName => Path.GetFileName(_project.FilePath);

        public string ProjectPath => Path.GetDirectoryName(_project.FilePath);

        public string DefaultNamespace => _project.DefaultNamespace;

        public async Task<List<ISymbol>> AllProjectSymbols()
        {
            List<ISymbol> ls = new List<ISymbol>();
            foreach (Document d in _project.Documents)
            {
                var m = await d.GetSemanticModelAsync();
                var root = await d.GetSyntaxRootAsync();

                if (root != null)
                {
                    List<ClassDeclarationSyntax> lc = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                    foreach (var c in lc)
                    {
                        ISymbol s = m.GetDeclaredSymbol(c);
                        ls.Add(s);
                    }

                    List<EnumDeclarationSyntax> enc = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();
                    foreach (var c in enc)
                    {
                        ISymbol s = m.GetDeclaredSymbol(c);
                        ls.Add(s);
                    }
                }
            }

            return ls;
        }

        public static async Task<WorkspaceProject> LoadFromSolution(Project project)
        {
            return project != null
                ? await LoadProject(_ => Task.FromResult(project))
                : null;
        }

        private static async Task<WorkspaceProject> LoadProject(Func<WorkspaceProgressBarProjectLoadStatus, Task<Project>> getProject)
        {
            var project = await getProject(new WorkspaceProgressBarProjectLoadStatus());
            var compilation = await project.GetCompilationAsync();

            compilation = compilation.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            var output = Path.Combine(Path.GetDirectoryName(project.FilePath), "bin");

            if (Directory.Exists(output))
            {
                var files = Directory.GetFiles(output, "*.dll").ToList();
                foreach (var f in files)
                {
                    await Console.Out.WriteLineAsync($"AddReference {f}");
                    compilation = compilation.AddReferences(MetadataReference.CreateFromFile(f));
                }
            }

            return new WorkspaceProject(project, compilation);
        }

        private WorkspaceProject(Project project, Compilation compilation)
        {
            _project = project;
            _compilation = compilation;
        }

        public async Task<List<AutoMapperCreateMap>> FindAutoMapper()
        {
            FindMapProfileCollector collector = new FindMapProfileCollector();
            try
            {
                foreach (var tree in _compilation.SyntaxTrees)
                {
                    var root = await tree.GetRootAsync() as CompilationUnitSyntax;
                    collector.Visit(root);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }

            return collector.MappingClassNamePairs.Select(x=> new AutoMapperCreateMap
            {
                SourceType = x.SourceType,
                DestinationType = x.DestinationType,
                ReverseMap = x.ReverseMap,
                FieldsMap = x.FieldsMappings,
                SyntaxNode = x.Node
            }).ToList();
        }
    }
}