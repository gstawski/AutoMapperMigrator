using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapperMigratorConsole.Interfaces;
using AutoMapperMigratorConsole.Model;
using AutoMapperMigratorConsole.WalkerCollectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Services;

public class AnalyzeSolutionService : IAnalyzeSolutionService
{
    private readonly AppConfiguration _configuration;
    private readonly ICodeTreeGeneratorService _codeTreeGeneratorService;

    private static async Task<List<WorkspaceAutoMapper>> GetMapperProfiles(WorkspaceSolution solution)
    {
        var profiles = await solution.FindAutoMapperProfiles();

        if (profiles.Count == 0)
        {
            Console.WriteLine("AutoMapper profiles not found!");
            return new List<WorkspaceAutoMapper>();
        }

        return profiles;
    }

    private static ISymbol FindClassSymbol(Dictionary<string, ISymbol> models, string className)
    {
        if (models.TryGetValue(className, out ISymbol model))
        {
            return model;
        }

        var nameWithDot = "." + className;
        foreach (var m in models)
        {
            if (m.Key.EndsWith(nameWithDot))
            {
                return m.Value;
            }
        }

        return null;
    }

    private static ClassDeclarationSyntax GetClassDeclarationSyntax(ISymbol symbol)
    {
        if (symbol is not ITypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
        {
            return null; // Not a class symbol
        }

        // Get the syntax references for the symbol
        var syntaxReferences = symbol.DeclaringSyntaxReferences;

        if (syntaxReferences.Length > 0)
        {
            // Get the syntax node from the first syntax reference
            var syntaxNode = syntaxReferences[0].GetSyntax();

            // Cast the syntax node to ClassDeclarationSyntax
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration;
            }
        }

        return null;
    }

    private static ClassDefinition GetClassDefinition(Dictionary<string, ISymbol> models, string className)
    {
        var properties = new Dictionary<string, PropertyDefinition>();

        var constructors = new List<ConstructorDefinition>();

        var symbolSource = FindClassSymbol(models, className);

        if (symbolSource != null)
        {
            var nameSpace = symbolSource.ContainingNamespace.ToString();

            var classDeclaration = GetClassDeclarationSyntax(symbolSource);

            FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector();
            publicPropertiesCollector.Visit(classDeclaration);
            if (publicPropertiesCollector.Properties.Count > 0)
            {
                foreach (var prop in publicPropertiesCollector.Properties)
                {
                    properties.Add(prop.Name.ToLower(), new PropertyDefinition(prop.Name, prop.Type, prop.Order, prop.IsSetPublic, prop.IsSimleType));
                }
            }

            FindConstructorsCollector findConstructorsCollector = new FindConstructorsCollector();
            findConstructorsCollector.Visit(classDeclaration);
            if (findConstructorsCollector.Constructors.Count > 0)
            {
                foreach (var mth in findConstructorsCollector.Constructors)
                {
                    var modelCollector = new FindModelsCollector();
                    modelCollector.Visit(mth);

                    var bodyCollector = new FindConstructorBodyAssignments();
                    bodyCollector.Visit(mth);

                    if (bodyCollector.FieldAssignments.Count == 0)
                    {
                        continue;
                    }

                    var ctorParams = GetPropertyDefinitions(modelCollector, bodyCollector.FieldAssignments);

                    if (ctorParams.Count > 0)
                    {
                        var ctor = new ConstructorDefinition
                        {
                            PropertiesAndTypes = ctorParams
                        };
                        constructors.Add(ctor);
                    }
                }
            }

            return new ClassDefinition(className, nameSpace, properties, constructors);
        }

        return new ClassDefinition(className, string.Empty, new Dictionary<string, PropertyDefinition>(), constructors);
    }

    private static Dictionary<string, PropertyDefinition> GetPropertyDefinitions(FindModelsCollector modelCollector, Dictionary<string, string> bodyCollectorFieldAssignments)
    {
        var ctorParams = new Dictionary<string, PropertyDefinition>();

        int order = 0;
        foreach (var m in modelCollector.FunctionParameters)
        {
            var typeName = m.Type?.ToString();
            var name = m.Identifier.Text;

            var isSimpleType = m.Type is PredefinedTypeSyntax ||
                               (m.Type is NullableTypeSyntax nullableType && nullableType.ElementType is PredefinedTypeSyntax);

            if (!isSimpleType)
            {
                if (typeName.EndsWith("DateTime") || typeName.EndsWith("DateTime?"))
                {
                    isSimpleType = true;
                }
                else if (typeName.EndsWith("DateTimeOffset") || typeName.EndsWith("DateTimeOffset?"))
                {
                    isSimpleType = true;
                }
                else if (typeName.EndsWith("TimeSpan") || typeName.EndsWith("TimeSpan?"))
                {
                    isSimpleType = true;
                }
            }

            if (bodyCollectorFieldAssignments.TryGetValue(name, out var assignment))
            {
                ctorParams.Add(assignment.ToLower(), new PropertyDefinition(name, typeName, ++order, assignment, isSimpleType));
            }
        }

        return ctorParams;
    }

    public AnalyzeSolutionService(AppConfiguration configuration, ICodeTreeGeneratorService codeTreeGeneratorService)
    {
        _configuration = configuration;
        _codeTreeGeneratorService = codeTreeGeneratorService;
    }

    public async Task AnalyzeSolution(string solutionPath)
    {
        // Load the solution and analyze it
        var solution = await WorkspaceSolution.Load(solutionPath);

        var models = await solution.AllProjectSymbols();

        var profiles = await GetMapperProfiles(solution);

        var solutionContext = new SolutionContext(models);

        foreach (var profile in profiles)
        {
            var space = profile.Project.DefaultNamespace;

            List<ClassMapDefinition> classMaps = new List<ClassMapDefinition>();

            foreach (var mapping in profile.Mappings)
            {
                var sourceType = GetClassDefinition(models, mapping.SourceType);
                var destinationType = GetClassDefinition(models, mapping.DestinationType);

                if (sourceType.HasProperties && destinationType.HasProperties)
                {
                    var clasMap = new ClassMapDefinition
                    {
                        SourceClass = sourceType,
                        DestinationClass = destinationType,
                        FieldsMap = mapping.FieldsMap
                    };

                    classMaps.Add(clasMap);

                    if (mapping.ReverseMap)
                    {
                        var reverseFields = mapping.FieldsMap
                            .Where(x=>x.SyntaxNode == null)
                            .Select(x => new AutoMapperFieldInfo
                        {
                            SourceField = x.DestinationField,
                            DestinationField = x.SourceField,
                            Ignore = x.Ignore,
                            SyntaxNode = x.SyntaxNode
                        }).ToList();

                        var clasMapReverse = new ClassMapDefinition
                        {
                            SourceClass = destinationType,
                            DestinationClass = sourceType,
                            FieldsMap = reverseFields
                        };

                        classMaps.Add(clasMapReverse);
                    }
                }
            }

            var mapperClass = _codeTreeGeneratorService.CreateMapper(solutionContext, space, classMaps);

            var code = mapperClass.NormalizeWhitespace().ToFullString();
            Console.WriteLine(code);


            if (!string.IsNullOrEmpty(_configuration.OutputPath))
            {
                var mappath = Path.Combine(profile.Project.ProjectPath, _configuration.OutputPath);

                if (!Directory.Exists(mappath))
                {
                    Directory.CreateDirectory(mappath);
                }

                if (!string.IsNullOrEmpty(_configuration.OutputFileName))
                {
                    var path = Path.Combine(mappath, _configuration.OutputFileName);
                    await File.WriteAllTextAsync(path, code, Encoding.UTF8);
                }
            }
        }
    }
}