using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapperMigratorConsole.Interfaces;
using AutoMapperMigratorConsole.Model;
using AutoMapperMigratorConsole.Rewriters;
using AutoMapperMigratorConsole.WalkerCollectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Services;

public sealed class CodeTreeGeneratorService : ICodeTreeGeneratorService
{
    private readonly AppConfiguration _appConfiguration;
    private readonly ICodeTreeConvertToTypeService _codeTreeConvertToTypeService;

    public CodeTreeGeneratorService(AppConfiguration appConfiguration, ICodeTreeConvertToTypeService codeTreeConvertToTypeService)
    {
        _appConfiguration = appConfiguration;
        _codeTreeConvertToTypeService = codeTreeConvertToTypeService;
    }

    private static ParameterSyntax SourceParameter(AppConfiguration configuration, ClassDefinition classDef, string identifier)
    {
        if (configuration.UseFullNameSpace)
        {
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(identifier))
                .WithType(SyntaxFactory.ParseTypeName(classDef.TypeNameWithNamespace));
        }

        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(identifier))
            .WithType(SyntaxFactory.ParseTypeName(classDef.TypeName));
    }

    private static IdentifierNameSyntax ReturnParameterType(AppConfiguration configuration, ClassDefinition classDef)
    {
        if (configuration.UseFullNameSpace)
        {
            return SyntaxFactory.IdentifierName(classDef.TypeNameWithNamespace);
        }

        return SyntaxFactory.IdentifierName(classDef.TypeName);
    }

    private static List<UsingDirectiveSyntax> CreateUsings(AppConfiguration appConfiguration, List<ClassMapDefinition> classMaps)
    {
        List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();

        if (appConfiguration.DefaultNameSpaces is { Count: > 0 })
        {
            foreach (var spaceName in appConfiguration.DefaultNameSpaces)
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(spaceName)));
            }
        }
        else
        {
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")));
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Globalization")));
        }

        Dictionary<string, byte> uniqueNames = new Dictionary<string, byte>();
        foreach (var classMap in classMaps)
        {
            if (uniqueNames.TryAdd(classMap.SourceClass.Namespace, 0))
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(classMap.SourceClass.Namespace)));
            }

            if (uniqueNames.TryAdd(classMap.DestinationClass.Namespace, 0))
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(classMap.DestinationClass.Namespace)));
            }
        }

        return usings;
    }

    private StatementSyntax CopyByConstructor(SolutionContext solutionContext, ClassMapDefinition classMap, Dictionary<string, ConvertFunctionDefinition> usedTypes)
    {
        var descClassName = classMap.DestinationClass.TypeName;
        if (_appConfiguration.UseFullNameSpace)
        {
            descClassName = classMap.DestinationClass.TypeNameWithNamespace;
        }

        var descProperties = classMap.DestinationClass.Constructors[0].PropertiesAndTypes;
        var sourceProperties = classMap.SourceClass.PropertiesAndTypes;

        List<(int Order, ArgumentSyntax Syntax)> argumentSyntaxes = new List<(int Order, ArgumentSyntax Syntax)>();

        int order = 0;
        foreach (var desc in descProperties)
        {
            if (sourceProperties.TryGetValue(desc.Key, out var source))
            {
                var leftArgument = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("source"),
                    SyntaxFactory.IdentifierName(source.Name));

                var argument = SyntaxFactory.Argument(leftArgument);

                if (TypesAreEqual(desc.Value, source))
                {
                    argumentSyntaxes.Add((++order, argument));
                }
                else
                {
                    var convert = _codeTreeConvertToTypeService.ConvertToType(solutionContext, desc.Value, source, leftArgument, usedTypes);
                    argumentSyntaxes.Add((++order, SyntaxFactory.Argument(convert)));
                }
            }
            else
            {
                argumentSyntaxes.Add((++order, SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
            }
        }

        var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier("desc")
                    )
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName(descClassName),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        argumentSyntaxes.OrderBy(x => x.Order).Select(x => x.Syntax).ToArray()
                                    )
                                ),
                                default
                            )
                        )
                    )
            )
        ));

        return variableDeclaration;
    }

    private static bool SimpleLambdaMapToSelf(SimpleLambdaExpressionSyntax simpleLambda)
    {
        var parameter = simpleLambda.Parameter.Identifier.Text;
        var expression = simpleLambda.Body.ToString();

        if (parameter == expression)
        {
            return true;
        }

        return false;
    }

    private static MemberAccessExpressionSyntax DestinationMember(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("desc"),
            SyntaxFactory.IdentifierName(memberName));
    }

    private static MemberAccessExpressionSyntax SourceMember(string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("source"),
            SyntaxFactory.IdentifierName(memberName));
    }

    private static void AssignProperties(List<StatementSyntax> statements, string left, string right)
    {
        var propertyCopy = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, DestinationMember(left), SourceMember(right)));
        statements.Add(propertyCopy);
    }

    private static void AssignProperties(List<StatementSyntax> statements, MemberAccessExpressionSyntax left, ExpressionSyntax right)
    {
        var propertyCopy = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right));
        statements.Add(propertyCopy);
    }

    private static void AddDestinationDeclaration(List<StatementSyntax> statements, string descClassName)
    {
        statements.Add(SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .AddVariables(SyntaxFactory.VariableDeclarator("desc")
                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(descClassName))
                            .WithArgumentList(SyntaxFactory.ArgumentList()))))));
    }

    private static ExpressionSyntax ReplaceMemberName(SyntaxNode sourceNode, SyntaxNode descNode)
    {
        FindMemberAccessExpression memberAccessExpression = new FindMemberAccessExpression();
        memberAccessExpression.Visit(sourceNode);

        var memberName = memberAccessExpression.MemberName;

        if (string.IsNullOrEmpty(memberName))
        {
            memberAccessExpression = new FindMemberAccessExpression();
            memberAccessExpression.Visit(descNode);

            memberName = memberAccessExpression.MemberName;
        }

        if (!string.IsNullOrEmpty(memberName) && memberName.Contains("."))
        {
            memberName = memberName.Split('.').First();
        }

        IdentifierReplacer replacer = new IdentifierReplacer(memberName, "source");
        var newCode = replacer.Visit(descNode);
        return newCode as ExpressionSyntax;
    }

    private void CopyByProperties(SolutionContext solutionContext, List<StatementSyntax> statements, ClassMapDefinition classMap, Dictionary<string, ConvertFunctionDefinition> usedTypes)
    {
        var descClassName = classMap.DestinationClass.TypeName;

        if (_appConfiguration.UseFullNameSpace)
        {
            descClassName = classMap.DestinationClass.TypeNameWithNamespace;
        }

        var descProperties = classMap.DestinationClass.PropertiesAndTypes;
        var sourceProperties = classMap.SourceClass.PropertiesAndTypes;

        AddDestinationDeclaration(statements, descClassName);

        var ignoreMap = classMap.FieldsMap
            .Where(x => x.Ignore)
            .ToDictionary(x => x.DestinationField.ToLower(), _ => 0);

        var definedMap = classMap.FieldsMap
            .Where(x => !string.IsNullOrEmpty(x.SourceField) && !x.AfterMap)
            .ToDictionary(x => x.DestinationField.ToLower());

        var codeMap = classMap.FieldsMap
            .Where(x => x.SyntaxNode != null && !x.AfterMap)
            .ToDictionary(x => x.DestinationField.ToLower(), y => y.SyntaxNode);

        foreach (var desc in descProperties)
        {
            if (ignoreMap.Count > 0 && ignoreMap.ContainsKey(desc.Key))
            {
                continue;
            }

            if (definedMap.Count > 0
                && definedMap.TryGetValue(desc.Key, out var dp)
                && sourceProperties.TryGetValue(dp.SourceField.ToLower(), out var source0))
            {
                if (TypesAreEqual(desc.Value, source0))
                {
                    AssignProperties(statements, desc.Value.Name, source0.Name);
                }
                else if (!dp.AfterMap && !string.IsNullOrEmpty(dp.Code))
                {
                    var functionCall = _codeTreeConvertToTypeService.CallMapFunction(solutionContext, desc.Value, source0);
                    AssignProperties(statements, DestinationMember(desc.Value.Name), functionCall);
                }
                else
                {
                    var convert = _codeTreeConvertToTypeService.ConvertToType(solutionContext, desc.Value, source0, SourceMember(source0.Name), usedTypes);
                    AssignProperties(statements, DestinationMember(desc.Value.Name), convert);
                }
            }
            else if (codeMap.Count > 0 && codeMap.TryGetValue(desc.Key, out var code))
            {
                if (code is ConditionalExpressionSyntax)
                {
                    AssignProperties(statements, DestinationMember(desc.Value.Name), ReplaceMemberName(code, code));
                }
                else if (code is InvocationExpressionSyntax invocation)
                {
                    AssignProperties(statements, DestinationMember(desc.Value.Name), ReplaceMemberName(invocation.ArgumentList, invocation));
                }
                else if (code is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    if (SimpleLambdaMapToSelf(simpleLambda))
                    {
                        var functionCall = _codeTreeConvertToTypeService.CallMapFunction(solutionContext, desc.Value);
                        AssignProperties(statements, DestinationMember(desc.Value.Name), functionCall);
                    }
                }
            }
            else if (sourceProperties.TryGetValue(desc.Key, out var source))
            {
                if (TypesAreEqual(desc.Value, source))
                {
                    AssignProperties(statements, desc.Value.Name, source.Name);
                }
                else
                {
                    var convert = _codeTreeConvertToTypeService.ConvertToType(solutionContext, desc.Value, source, SourceMember(source.Name), usedTypes);
                    AssignProperties(statements, DestinationMember(desc.Value.Name), convert);
                }
            }
            else if (definedMap.Count == 0)
            {
                if (desc.Value.Type == "string" || desc.Value.Type.EndsWith("?"))
                {
                    var propertyCopy = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("desc"),
                                SyntaxFactory.IdentifierName(desc.Value.Name)
                            ),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
                    statements.Add(propertyCopy);
                }
                else
                {
                    var commentedLine = SyntaxFactory.Comment($"// desc.{desc.Value.Name} = null;");
                    statements.Add(SyntaxFactory.ParseStatement(string.Empty).WithLeadingTrivia(commentedLine));
                }
            }
        }

        foreach (var afterMap in classMap.FieldsMap.Where(x => x.AfterMap))
        {
            if (!string.IsNullOrEmpty(afterMap.Code))
            {
                var exp = SyntaxFactory.ParseStatement(afterMap.Code);
                statements.Add(exp);
            }
        }
    }

    private static bool TypesAreEqual(PropertyDefinition desc, PropertyDefinition source)
    {
        if (!source.IsSimpleType)
        {
            return false;
        }

        if (desc.Type == source.Type)
        {
            return true;
        }

        if (desc.RawType == source.RawType)
        {
            return true;
        }

        if (!source.IsNullable && source.RawType + "?" == desc.RawType)
        {
            return true;
        }

        if (source.Type.StartsWith("string", StringComparison.InvariantCultureIgnoreCase) && desc.Type.StartsWith("string", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private MethodDeclarationSyntax GetMapperDeclarationSyntax(SolutionContext solutionContext, ClassMapDefinition classMap, Dictionary<string, ConvertFunctionDefinition> usedTypes)
    {
        var sourceClass = classMap.SourceClass;
        var descClass = classMap.DestinationClass;

        List<StatementSyntax> statements = new List<StatementSyntax>();

        if (descClass.HasPrivateSetProperties && descClass.Constructors.Count > 0)
        {
            statements.Add(CopyByConstructor(solutionContext, classMap, usedTypes));
        }
        else
        {
            CopyByProperties(solutionContext, statements, classMap, usedTypes);
        }

        statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("desc")));

        var body = SyntaxFactory.Block(statements);

        var methodDeclaration = SyntaxFactory.MethodDeclaration(
                ReturnParameterType(_appConfiguration, descClass),
                SyntaxFactory.Identifier(solutionContext.GetFunctionName(descClass.TypeNameWithNamespace, sourceClass.TypeNameWithNamespace)))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(SourceParameter(_appConfiguration, sourceClass, "source"))
            .WithBody(body);

        return methodDeclaration;
    }

    public CompilationUnitSyntax CreateMapper(SolutionContext solutionContext)
    {
        var usings = CreateUsings(_appConfiguration, solutionContext.ClassMaps);

        List<MemberDeclarationSyntax> methods = new List<MemberDeclarationSyntax>();

        var nameSpaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(
                SyntaxFactory.ParseName(solutionContext.DefaultNamespace))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.SemicolonToken, SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        methods.Add(nameSpaceDeclaration);

        var usedTypes = new Dictionary<string, ConvertFunctionDefinition>();

        List<MemberDeclarationSyntax> mappers = new List<MemberDeclarationSyntax>();

        foreach (var classMap in solutionContext.ClassMaps)
        {
            var mapper = GetMapperDeclarationSyntax(solutionContext, classMap, usedTypes);
            mappers.Add(mapper);
        }

        var classMethods = _codeTreeConvertToTypeService.GetConvertFunctionsBodies(usedTypes);
        classMethods.AddRange(mappers);

        var mapperClass = SyntaxFactory.ClassDeclaration(_appConfiguration.MapperClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddMembers(classMethods.ToArray())
            ;

        methods.Add(mapperClass);

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(new SyntaxList<UsingDirectiveSyntax>(usings))
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(methods.ToArray()));

        return compilationUnit;
    }
}