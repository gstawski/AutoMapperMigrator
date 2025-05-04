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

    private static ClassDefinition GetClassDefinition(SolutionContext solutionContext, string className)
    {
        var properties = new Dictionary<string, PropertyDefinition>();

        var constructors = new List<ConstructorDefinition>();

        var symbolSource = solutionContext.FindClassSymbol(className);

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

    private static void CheckComplexTypesInPropertyList(SolutionContext solutionContext, AppConfiguration appConfiguration, ClassDefinition destinationType, List<AutoMapperFieldInfo> fieldsMap)
    {
        foreach (var prp in destinationType.PropertiesAndTypes.Values)
        {
            if (!prp.IsSimpleType)
            {
                if (prp.Type.Contains("<"))
                {
                    var split = prp.Type.Split(new[] { '<', '>', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length == 2)
                    {
                        var t = split[1];
                        if (solutionContext.TryGetSolutionSymbol(t) != null)
                        {
                            TryMatchSourceType(solutionContext, appConfiguration, prp, t, fieldsMap);
                        }
                    }
                }
                else
                {
                    if (solutionContext.TryGetSolutionSymbol(prp.Type) != null)
                    {
                        TryMatchSourceType(solutionContext, appConfiguration, prp, prp.Type, fieldsMap);
                    }
                }
            }
        }
    }

    private static void TryMatchSourceType(SolutionContext solutionContext, AppConfiguration appConfiguration, PropertyDefinition propertyDefinition, string destinationType, List<AutoMapperFieldInfo> fieldsMap)
    {
        var postfixes = appConfiguration.SearchClassPostfixes;

        foreach (var postfix in postfixes)
        {
            if (destinationType.Contains(postfix, StringComparison.InvariantCultureIgnoreCase))
            {
                var ntype = destinationType.Replace(postfix, string.Empty, StringComparison.InvariantCultureIgnoreCase);
                var sourceType = solutionContext.TryGetSolutionSymbol(ntype);
                if (sourceType != null)
                {
                    fieldsMap.Add(new AutoMapperFieldInfo
                    {
                        AfterMap = false,
                        DestinationField = propertyDefinition.Name,
                        Code = $"{appConfiguration.MapFunctionNamesPrefix}{destinationType}(source);",
                    });

                    CreateMapDefinition(solutionContext, appConfiguration, ntype, destinationType);
                    return;
                }
            }
        }

        foreach (var postfix in postfixes)
        {
            var ntype = destinationType + postfix;
            var sourceType = solutionContext.TryGetSolutionSymbol(ntype);
            if (sourceType != null)
            {
                fieldsMap.Add(new AutoMapperFieldInfo
                {
                    AfterMap = false,
                    DestinationField = propertyDefinition.Name,
                    Code = $"{appConfiguration.MapFunctionNamesPrefix}{destinationType}(source);",
                });

                CreateMapDefinition(solutionContext, appConfiguration, ntype, destinationType);
                return;
            }
        }
    }

    private static void CreateMapDefinition(SolutionContext solutionContext, AppConfiguration appConfiguration, string sourceClass, string destinationClass)
    {
        var sourceType = GetClassDefinition(solutionContext, sourceClass);
        var destinationType = GetClassDefinition(solutionContext, destinationClass);

        List<AutoMapperFieldInfo> fieldsMap = new List<AutoMapperFieldInfo>();

        CheckComplexTypesInPropertyList(solutionContext, appConfiguration, destinationType, fieldsMap);

        foreach (var prp in destinationType.PropertiesAndTypes)
        {
            if (sourceType.PropertiesAndTypes.TryGetValue(prp.Key, out var value))
            {
                fieldsMap.Add(new AutoMapperFieldInfo
                {
                    SourceField = value.Name,
                    DestinationField = prp.Value.Name,
                });
            }
            else
            {
                foreach (var prp1 in sourceType.PropertiesAndTypes.Values)
                {
                    if (!prp1.IsSimpleType && prp1.Type == prp.Value.Type)
                    {
                        fieldsMap.Add(new AutoMapperFieldInfo
                        {
                            SourceField = prp1.Name,
                            DestinationField = prp.Value.Name,
                        });
                        break;
                    }
                }
            }
        }

        if (fieldsMap.Count > 0)
        {
            var clasMap = new ClassMapDefinition
            {
                SourceClass = sourceType,
                DestinationClass = destinationType,
                FieldsMap = fieldsMap
            };

            solutionContext.ClassMaps.Add(clasMap);
        }
    }

    private static void OutputCodeToConsole(string code)
    {
        Console.WriteLine(String.Empty);
        Console.WriteLine(String.Empty);
        Console.WriteLine(code);
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

        foreach (var profile in profiles)
        {
            var space = profile.Project.DefaultNamespace;

            var solutionContext = new SolutionContext(models, space);

            List<ClassMapDefinition> classMaps = solutionContext.ClassMaps;

            foreach (var mapping in profile.Mappings)
            {
                var sourceType = GetClassDefinition(solutionContext, mapping.SourceType);
                var destinationType = GetClassDefinition(solutionContext, mapping.DestinationType);

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
                            .Where(x => x.SyntaxNode == null)
                            .Select(x => new AutoMapperFieldInfo
                            {
                                SourceField = x.DestinationField,
                                DestinationField = x.SourceField,
                                Ignore = x.Ignore,
                                SyntaxNode = x.SyntaxNode
                            })
                            .ToList();

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

            var mapperClass = _codeTreeGeneratorService.CreateMapper(solutionContext);

            var code = mapperClass.NormalizeWhitespace().ToFullString();
            OutputCodeToConsole(code);

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

    public async Task AnalyzeForOneMap(string solutionPath, string sourceClass, string destinationClass)
    {
        var solution = await WorkspaceSolution.Load(solutionPath);

        var models = await solution.AllProjectSymbols();

        var solutionContext = new SolutionContext(models, "Test");

        var sourceSymbol = solutionContext.TryGetSolutionSymbol(sourceClass);

        if (sourceSymbol == null)
        {
            throw new Exception("Source symbol not found");
        }

        var destinationSymbol = solutionContext.TryGetSolutionSymbol(destinationClass);

        if (destinationSymbol == null)
        {
            throw new Exception("Destination symbol not found");
        }

        CreateMapDefinition(solutionContext, _configuration, sourceClass, destinationClass);

        var mapperClass = _codeTreeGeneratorService.CreateMapper(solutionContext);

        var code = mapperClass.NormalizeWhitespace().ToFullString();

        OutputCodeToConsole(code);
    }
}