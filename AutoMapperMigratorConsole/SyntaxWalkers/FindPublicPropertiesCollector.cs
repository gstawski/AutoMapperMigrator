using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.SyntaxWalkers;

public sealed class FindPublicPropertiesCollector : CSharpSyntaxWalker
{
    private int _order;
    public ICollection<(string Name, string Type, int Order, bool IsSetPublic, bool IsSimleType)> Properties { get; } = new List<(string Name, string Type, int Order, bool IsSetPublic, bool IsSimleType)>();

    private bool IsPublic(AccessorDeclarationSyntax accessor)
    {
        foreach (SyntaxToken token in accessor.Modifiers)
        {
            if (token.IsKind(SyntaxKind.PrivateKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword))
            {
                return false;
            }
        }
        return true;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var isPublic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
        var isStatic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
        var isSetPublic = false;

        if (node.AccessorList != null && node.AccessorList.Accessors.Count > 0)
        {
            foreach (var accessor in node.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) && IsPublic(accessor))
                {
                    isSetPublic = true;
                }
            }
        }

        if (isPublic && !isStatic)
        {
            var name = node.Identifier.Text;
            var typeName = node.Type.ToString();

            var isSimpleType = node.Type is PredefinedTypeSyntax ||
                               (node.Type is NullableTypeSyntax nullableType && nullableType.ElementType is PredefinedTypeSyntax);

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

            Properties.Add((name, typeName, ++_order, isSetPublic, isSimpleType));
        }
    }
}