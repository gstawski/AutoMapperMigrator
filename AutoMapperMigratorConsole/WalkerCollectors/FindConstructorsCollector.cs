using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.WalkerCollectors;

public class FindConstructorsCollector : CSharpSyntaxWalker
{
    public ICollection<ConstructorDeclarationSyntax> Constructors { get; } = new List<ConstructorDeclarationSyntax>();

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Constructors.Add(node);
    }
}