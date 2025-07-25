﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.SyntaxWalkers;

internal sealed class FindModelsCollector : CSharpSyntaxWalker
{
    public ICollection<ParameterSyntax> FunctionParameters { get; } = new List<ParameterSyntax>();
    
    public override void VisitParameter(ParameterSyntax node)
    {
        FunctionParameters.Add(node);
        base.VisitParameter(node);
    }
}