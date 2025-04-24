using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.WalkerCollectors;

public class AutoMapperMappingsWalker : CSharpSyntaxWalker
{
    private readonly List<AutoMapperFieldInfo> _mappings = new();
    private readonly Dictionary<string, byte> _uniquePropertyCheck = new();

    public ICollection<AutoMapperFieldInfo> Mappings => _mappings;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax mae1 &&
            mae1.Name.Identifier.Text == "ForMember")
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count == 2)
            {
                var destExpression = args[0].Expression as LambdaExpressionSyntax;
                var sourceExpression = args[1]
                    .Expression
                    .DescendantNodes()
                    .OfType<LambdaExpressionSyntax>()
                    .LastOrDefault();

                if (destExpression?.Body is MemberAccessExpressionSyntax destMae)
                {
                    if (sourceExpression?.Body is MemberAccessExpressionSyntax sourceMae)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae.Name.Identifier.Text,
                            DestinationField = destMae.Name.Identifier.Text
                        };

                        if (UniquenessCheck(mapping))
                        {
                            return;
                        }

                        _mappings.Add(mapping);
                    }
                    if (sourceExpression?.Body is ConditionalExpressionSyntax conditionalExpressionSyntax)
                    {
                        _mappings.Add(
                            new AutoMapperFieldInfo
                            {
                                SourceField = string.Empty,
                                DestinationField = destMae.Name.Identifier.Text,
                                Ignore = false,
                                SyntaxNode = conditionalExpressionSyntax
                            }
                        );
                    }
                    if (sourceExpression?.Body is InvocationExpressionSyntax expressionSyntax)
                    {
                        _mappings.Add(
                            new AutoMapperFieldInfo
                            {
                                SourceField = string.Empty,
                                DestinationField = destMae.Name.Identifier.Text,
                                Ignore = false,
                                SyntaxNode = expressionSyntax
                            }
                        );
                    }
                }

                var destLambda = args[0].Expression as SimpleLambdaExpressionSyntax;
                var sourceLambda = args[1].Expression as SimpleLambdaExpressionSyntax;

                if (destLambda != null && sourceLambda != null)
                {
                    if (destLambda?.Body is MemberAccessExpressionSyntax destMae2 &&
                        sourceLambda?.Body is MemberAccessExpressionSyntax sourceMae2)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae2.Name.Identifier.Text,
                            DestinationField = destMae2.Name.Identifier.Text
                        };

                        if (UniquenessCheck(mapping))
                        {
                            return;
                        }

                        _mappings.Add(mapping);
                    }
                }
            }
        }
        else if (node.Expression is MemberAccessExpressionSyntax mae2 &&
                 mae2.Name.Identifier.Text == "ForPath")
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count == 2)
            {
                var destExpression = args[0].Expression as LambdaExpressionSyntax;
                var sourceExpression = args[1]
                    .Expression
                    .DescendantNodes()
                    .OfType<LambdaExpressionSyntax>()
                    .LastOrDefault();

                if (destExpression?.Body is MemberAccessExpressionSyntax destMae1 &&
                    sourceExpression?.Body is MemberAccessExpressionSyntax sourceMae1)
                {
                    var mapping = new AutoMapperFieldInfo
                    {
                        SourceField = sourceMae1.Name.Identifier.Text,
                        DestinationField = destMae1.Name.Identifier.Text
                    };
                    if (UniquenessCheck(mapping))
                    {
                        return;
                    }
                    _mappings.Add(mapping);
                }

                var destLambda = args[0].Expression as SimpleLambdaExpressionSyntax;
                var sourceLambda = args[1].Expression as SimpleLambdaExpressionSyntax;

                if (destLambda != null && sourceLambda != null)
                {
                    if (destLambda?.Body is MemberAccessExpressionSyntax destMae2 &&
                        sourceLambda?.Body is MemberAccessExpressionSyntax sourceMae2)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae2.Name.Identifier.Text,
                            DestinationField = destMae2.Name.Identifier.Text
                        };

                        if (UniquenessCheck(mapping))
                        {
                            return;
                        }

                        _mappings.Add(mapping);
                    }
                    else if (destLambda?.Body is MemberAccessExpressionSyntax destMae3 &&
                             sourceLambda?.Body is InvocationExpressionSyntax sourceMae3 &&
                             (sourceMae3.Expression as MemberAccessExpressionSyntax)?.Name.ToString() == "Ignore")
                    {
                        _mappings.Add(
                            new AutoMapperFieldInfo
                            {
                                SourceField = string.Empty,
                                DestinationField = destMae3.Name.Identifier.Text,
                                Ignore = true
                            }
                        );
                    }
                }
            }
        }

        base.VisitInvocationExpression(node);
    }

    private bool UniquenessCheck(AutoMapperFieldInfo mapping)
    {
        if (!_uniquePropertyCheck.TryAdd($"{mapping.SourceField}-{mapping.DestinationField}", 0))
        {
            Console.WriteLine($"Duplicate mapping found: {mapping.SourceField} - {mapping.DestinationField}");
            return true;
        }

        return false;
    }
}