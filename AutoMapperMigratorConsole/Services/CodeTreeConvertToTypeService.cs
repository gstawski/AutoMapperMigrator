﻿using System;
using System.Collections.Generic;
using AutoMapperMigratorConsole.Interfaces;
using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Services;

public class CodeTreeConvertToTypeService : ICodeTreeConvertToTypeService
{
    private readonly AppConfiguration _appConfiguration;

    private static Dictionary<string, byte> _primitiveTypes = new Dictionary<string, byte>
    {
        { "int", 1 },
        { "long", 2 },
        { "double", 3 },
        { "float", 4 },
        { "decimal", 5 },
        { "bool", 6 },
        { "DateTime", 9 },
        { "DateTime?", 10 },
        { "Guid", 11 },
        { "Guid?", 12 },
        { "string", 13 },
        { "string?", 14 },
        { "byte", 15 },
        { "byte?", 16 },
        { "short", 17 },
        { "short?", 18 },
        { "char", 19 },
        { "char?", 20 },
        { "sbyte", 21 },
        { "sbyte?", 22 },
        { "ushort", 23 },
        { "ushort?", 24 },
        { "uint", 25 },
        { "uint?", 26 },
        { "ulong", 27 },
        { "ulong?", 28 }
    };

    public CodeTreeConvertToTypeService(AppConfiguration appConfiguration)
    {
        _appConfiguration = appConfiguration;
    }

    private static bool IsEnumType(ISymbol symbol)
    {
        if (symbol == null)
        {
            return false;
        }

        if (symbol.Kind == SymbolKind.NamedType)
        {
            if (symbol is ITypeSymbol typeSymbol)
            {
                return typeSymbol.TypeKind == TypeKind.Enum;
            }
        }

        return false;
    }

    private static string NormalizeType(string name)
    {
        if (name.Contains("."))
        {
            var index = name.LastIndexOf('.');
            name = name.Substring(index + 1);
        }

        return name;
    }

    private static InvocationExpressionSyntax CallFunction(string function, ExpressionSyntax expression)
    {
        return
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(function),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                        SyntaxFactory.Argument(expression)
                    )
                )
            );
    }

    private static InvocationExpressionSyntax CallGenericFunction(string function, string typ1, string typ2, ExpressionSyntax expression)
    {
        return
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(function))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SeparatedList<TypeSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                        SyntaxFactory.IdentifierName(typ1),
                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                        SyntaxFactory.IdentifierName(typ2)
                                    }))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                            SyntaxFactory.Argument(expression)
                        )
                    )
                );
    }

    public ExpressionSyntax ConvertToType(SolutionContext solutionContext, PropertyDefinition destination, PropertyDefinition source, ExpressionSyntax expression, Dictionary<string, ConvertFunctionDefinition> usedTypes)
    {
        var destinationType = destination.Type;
        var sourceType = source.Type;
        var functionKey = $"{sourceType.ToLower()}-{destination.Type.ToLower()}";

        var functions = _appConfiguration.ConvertFunctions;

        if (functions.TryGetValue(functionKey, out var f))
        {
            usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition(f.FunctionName, f.FunctionBody, false));
            return CallFunction(f.FunctionName, expression);
        }

        if (source.Type != source.RawType || destination.Type != destination.RawType)
        {
            functionKey = $"{source.RawType.ToLower()}-{destination.RawType.ToLower()}";
            if (functions.TryGetValue(functionKey, out var fs))
            {
                usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition(fs.FunctionName, fs.FunctionBody, false));
                return CallFunction(fs.FunctionName, expression);
            }
        }

        if (destination.Type.ToLower() == "string?")
        {
            functionKey = $"{sourceType.ToLower()}-string";
            if (functions.TryGetValue(functionKey, out var f2))
            {
                usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition(f2.FunctionName, f2.FunctionBody, false));
                return CallFunction(f2.FunctionName, expression);
            }
        }

        var destinationSymbol = solutionContext.TryGetSolutionSymbol(destination.Type);
        var sourceSymbol = solutionContext.TryGetSolutionSymbol(sourceType);

        if (IsEnumType(destinationSymbol) && IsEnumType(sourceSymbol))
        {
            if (source.IsNullable)
            {
                functionKey = "CastEnumNullable";
                if (functions.TryGetValue(functionKey, out var f2))
                {
                    usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition(f2.FunctionName, f2.FunctionBody, false));
                    return CallGenericFunction(f2.FunctionName, sourceType, destinationType, expression);
                }
            }

            {
                functionKey = "CastEnum";
                if (functions.TryGetValue(functionKey, out var f2))
                {
                    usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition(f2.FunctionName, f2.FunctionBody, false));
                    return CallGenericFunction(f2.FunctionName, sourceType, destinationType, expression);
                }
            }
        }

        if (destinationType == "string" || destinationType == "string?")
        {
            if (IsEnumType(sourceSymbol))
            {
                var code = $@"
    private static {destinationType} ToString({sourceType} en)
    {{
        if (en.HasValue)
        {{
            return Enum.GetName(en.Value.GetType(), en);
        }}

        return null;
    }}
";
                usedTypes.TryAdd(functionKey, new ConvertFunctionDefinition("ToString", code, false));
                return CallFunction("ToString", expression);
            }
        }

        if (solutionContext.IsSolutionType(destinationType))
        {
            var function = "Map" + destinationType;
            return CallFunction(function, expression);
        }

        if (destinationType.Contains("<"))
        {
            var collectionTypes = _appConfiguration.CollectionTypes;

            var split = destinationType.Split(new[] { '<', '>', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                var ntype = NormalizeType(split[0]);

                if (collectionTypes.ContainsKey(ntype))
                {
                    var ltype = split[1];

                    if (_appConfiguration.UseFullNameSpace)
                    {
                        var lSymbol = solutionContext.TryGetSolutionSymbol(ltype);
                        if (lSymbol != null)
                        {
                            ltype = lSymbol.ToDisplayString();
                        }
                    }

                    if (_primitiveTypes.ContainsKey(ltype))
                    {
                        var code = $"source.{source.Name} != null ? source.{source.Name}.ToList() : new List<{ltype}>()";
                        var exp = SyntaxFactory.ParseExpression(code);
                        return exp;
                    }
                    else
                    {
                        var code = $"source.{source.Name} != null ? source.{source.Name}.Select(Map{split[1]}).ToList() : new List<{ltype}>()";
                        var exp = SyntaxFactory.ParseExpression(code);
                        return exp;
                    }
                }

                if (destinationType.Contains("Dictionary") && split.Length > 2)
                {
                    var type1 = split[1];
                    var type2 = split[2];

                    var code = $"source.{source.Name} != null ? source.{source.Name}.ToDictionary(x => x.Key, y => y.Value) : new Dictionary<{type1}, {type2}>()";
                    var exp = SyntaxFactory.ParseExpression(code);
                    return exp;
                }
            }
        }

        if (destinationType.EndsWith("[]"))
        {
            var ntype = destinationType.TrimEnd('[', ']');

            if (_primitiveTypes.ContainsKey(ntype))
            {
                var code = $"source.{source.Name}.Cast<{ntype}>().ToArray()";
                var exp = SyntaxFactory.ParseExpression(code);
                return exp;
            }

            if (solutionContext.IsSolutionType(ntype))
            {
                var code = $"source.{source.Name} != null ? source.{source.Name}.Select(Map{ntype}).ToArray() : new List<{ntype}>().ToArray()";
                var exp = SyntaxFactory.ParseExpression(code);
                return exp;
            }
        }

        return CallFunction("Unknown", expression);
    }

    public List<MemberDeclarationSyntax> GetConvertFunctionsBodies(Dictionary<string, ConvertFunctionDefinition> usedTypes)
    {
        List<MemberDeclarationSyntax> classMethods = new List<MemberDeclarationSyntax>();
        foreach (var t in usedTypes.Values)
        {
            var method = SyntaxFactory.ParseMemberDeclaration(t.Body);
            classMethods.Add(method);
        }

        return classMethods;
    }
}