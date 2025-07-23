using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Rewriters;

public sealed class IdentifierReplacer : CSharpSyntaxRewriter
{
    private readonly string _oldSymbolName;
    private readonly string _newSymbolName;

    public IdentifierReplacer(string oldSymbolName, string newSymbolName)
    {
        _oldSymbolName = oldSymbolName;
        _newSymbolName = newSymbolName;
    }

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Identifier.Text == _oldSymbolName)
        {
            return node.WithIdentifier(SyntaxFactory.ParseToken(_newSymbolName));
        }

        return base.VisitIdentifierName(node);
    }
}