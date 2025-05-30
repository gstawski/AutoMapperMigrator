﻿using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Interfaces;

public interface ICodeTreeGeneratorService
{
    CompilationUnitSyntax CreateMapper(SolutionContext solutionContext);
}