using System.Collections.Generic;
using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Interfaces;

public interface ICodeTreeConvertToTypeService
{
    ExpressionSyntax CallMapFunction(SolutionContext solutionContext, PropertyDefinition destination);

    ExpressionSyntax ConvertToType(SolutionContext solutionContext, PropertyDefinition destination, PropertyDefinition source, ExpressionSyntax expression, Dictionary<string, ConvertFunctionDefinition> usedTypes);

    List<MemberDeclarationSyntax> GetConvertFunctionsBodies(Dictionary<string, ConvertFunctionDefinition> usedTypes);
}