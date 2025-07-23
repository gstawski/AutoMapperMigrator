using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.SyntaxWalkers;

public sealed class FindMemberAccessExpression : CSharpSyntaxWalker
{
    public string MemberName { get; private set; }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (string.IsNullOrWhiteSpace(MemberName))
        {
            MemberName = node.Expression.ToString();
        }

        base.VisitMemberAccessExpression(node);
    }
}