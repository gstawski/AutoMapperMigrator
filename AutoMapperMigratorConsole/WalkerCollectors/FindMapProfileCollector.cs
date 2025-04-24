using System.Collections.Generic;
using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.WalkerCollectors;

public class FindMapProfileCollector : CSharpSyntaxWalker
{
    public List<(string SourceType, string DestinationType, bool ReverseMap, ICollection<AutoMapperFieldInfo> FieldsMappings, ExpressionStatementSyntax Node)> MappingClassNamePairs { get; } = new ();
    private bool _isProfileClass;

    // Check class declaration for Profile base class
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Reset flag for each new class
        _isProfileClass = false;

        // Check base types
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                string baseTypeName = baseType.Type.ToString();
                if (baseTypeName == "Profile")
                {
                    _isProfileClass = true;
                    break;
                }
            }
        }

        // Only visit children if this is a Profile-derived class
        if (_isProfileClass)
        {
            base.VisitClassDeclaration(node);
        }
    }

    private static ExpressionStatementSyntax FindParent(SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent is ExpressionStatementSyntax)
        {
            return parent as ExpressionStatementSyntax;
        }
        return FindParent(parent);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Only process if we're in a Profile-derived class
        if (_isProfileClass)
        {
            if (node.Expression is GenericNameSyntax genericName1 && genericName1.Identifier.Text == "CreateMap")
            {
                // Extract the type arguments (source and destination classes)
                var typeArguments = genericName1.TypeArgumentList.Arguments;
                if (typeArguments.Count == 2)
                {
                    var parent = FindParent(node);
                    AutoMapperMappingsWalker w = new();
                    w.Visit(parent);

                    string sourceType = typeArguments[0].ToString();
                    string destinationType = typeArguments[1].ToString();
                    bool reverseMap = parent.ToString().Contains("ReverseMap()");
                    MappingClassNamePairs.Add((sourceType, destinationType, reverseMap, w.Mappings, parent));
                }
            }
        }

        base.VisitInvocationExpression(node);
    }
}