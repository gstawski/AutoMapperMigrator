using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.WalkerCollectors;

public sealed class FindConstructorBodyAssignments : CSharpSyntaxWalker
{
    public Dictionary<string, string> FieldAssignments { get; } = new();

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        if (node.Kind() == SyntaxKind.SimpleAssignmentExpression)
        {
            string leftMemberName = string.Empty;
            string rightMemberName = string.Empty;

            var left = node.Left;
            var right = node.Right;

            if (left.Kind() == SyntaxKind.IdentifierName)
            {
                var identifier = (IdentifierNameSyntax)left;
                leftMemberName = identifier.Identifier.ValueText;
            }
            else if (left.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var memberAccess = (MemberAccessExpressionSyntax)left;
                leftMemberName = memberAccess.Name.Identifier.ValueText;
            }

            if (right.Kind() == SyntaxKind.IdentifierName)
            {
                var identifier = (IdentifierNameSyntax)right;
                rightMemberName = identifier.Identifier.ValueText;
            }

            if (!string.IsNullOrEmpty(leftMemberName) && !string.IsNullOrEmpty(rightMemberName))
            {
                FieldAssignments.Add(rightMemberName, leftMemberName);
            }
        }

        base.VisitAssignmentExpression(node);
    }
}